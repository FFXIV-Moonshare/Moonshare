using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Moonshare_Plugin
{
    public class UserSessionManager : IDisposable
    {
        private ClientWebSocket? playerSocket;
        private CancellationTokenSource? cts;
        private readonly IPluginLog log;

        public string LocalUserId { get; private set; }
        public string? SessionToken { get; private set; }
        public string? ConnectedToUserId { get; private set; }

        public bool IsConnected => playerSocket?.State == WebSocketState.Open;

        public Dictionary<string, string> ConnectedUsers { get; private set; } = new();
        public string? ConnectedPlayerServerUrl { get; private set; }
        public event Action<string>? OnInstanceInfoReceived;

        public string? ConnectedInstanceName { get; private set; }
        public event Action? OnUserListChanged;
        public event Action<string?, bool>? OnConnectionStatusChanged;

        // Upload-Fortschritt-Event (0..100)
        public event Action<int>? OnUploadProgress;

        private const int ChunkSize = 32 * 1024; // 32 KB

        private TaskCompletionSource<bool>? fileSendReadyTcs;

        public UserSessionManager(IPluginLog log)
        {
            this.log = log;
            LocalUserId = Guid.NewGuid().ToString();
        }

        // Sharding-Logik: Ermittelt PlayerServer-URL anhand der UserId
        private string GetPlayerServerUrl()
        {
            int shardCount = 3; // Anzahl PlayerServer-Instanzen, ggf. anpassen
            int shardIndex = GetStableHash(LocalUserId) % shardCount;
            int port = 5000 + shardIndex;
            return $"ws://62.68.75.23:{port}/player";
        }

       public async Task InitializeAsync(int maxRetries = 5, int delayMs = 2000)
{
    int attempt = 0;

    while (attempt < maxRetries)
    {
        attempt++;

        try
        {
            log.Information($"🔑 Using UserId: {LocalUserId}");

            var authToken = await GetAuthTokenFromHttpAsync(LocalUserId);
            if (authToken == null)
            {
                log.Error("❌ Authentication failed: no token received.");
                return;
            }
            SessionToken = authToken;

            log.Information($"✅ Authenticated as {LocalUserId} with token: {authToken}");

            cts?.Cancel();
            cts = new CancellationTokenSource();

            playerSocket?.Dispose();
            playerSocket = new ClientWebSocket();

                    var playerUrl = GetPlayerServerUrl();
                    ConnectedPlayerServerUrl = playerUrl; // merken

                    var playerUri = new Uri(playerUrl);
                    log.Information($"🌐 Connecting to PlayerServer at {playerUri}... (Attempt {attempt}/{maxRetries})");
                    await playerSocket.ConnectAsync(playerUri, cts.Token);
                    log.Information("✅ Connected to PlayerServer.");

                    var authMsg = JsonSerializer.Serialize(new
            {
                type = "session_auth",
                userId = LocalUserId,
                token = SessionToken
            });
            var authBytes = Encoding.UTF8.GetBytes(authMsg);
            await playerSocket.SendAsync(authBytes, WebSocketMessageType.Text, true, cts.Token);

            _ = Task.Run(() => ReceiveLoop(cts.Token), cts.Token);

            return; // Erfolg, raus aus der Retry-Schleife
        }
        catch (Exception ex)
        {
            log.Warning(ex, $"⚠️ Connection attempt {attempt} failed.");
            if (attempt < maxRetries)
            {
                log.Information($"⏳ Waiting {delayMs}ms before retry...");
                await Task.Delay(delayMs);
            }
            else
            {
                log.Error("❌ All connection attempts failed.");
            }
        }
    }
}

        private async Task<string?> GetAuthTokenFromHttpAsync(string userId)
        {
            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync($"http://62.68.75.23:5003/sessions?userId={userId}");

                if (!response.IsSuccessStatusCode)
                {
                    log.Error($"❌ Auth server HTTP error: {response.StatusCode}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("token", out var tokenElement))
                {
                    return tokenElement.GetString();
                }

                log.Error("❌ No 'token' field in auth server response.");
                return null;
            }
            catch (Exception ex)
            {
                log.Error(ex, "❌ Error connecting to AuthServer");
                return null;
            }
        }

        private async Task HandleServerMessageAsync(string message)
        {
            if (message == "file_receive_ready")
            {
                fileSendReadyTcs?.TrySetResult(true);
            }
            else if (message.StartsWith("FILE_FAILED"))
            {
                fileSendReadyTcs?.TrySetResult(false);
            }
        }

        public async Task SendFileAsync(string targetUserId, byte[] fileBytes, string fileName)
        {
            if (!IsConnected)
            {
                log.Warning("⚠️ Not connected, cannot send file.");
                return;
            }

            var header = new
            {
                type = "file_send_begin",
                targetUserId,
                fileName,
                fileSize = fileBytes.Length
            };
            string headerJson = JsonSerializer.Serialize(header);
            var headerBytes = Encoding.UTF8.GetBytes(headerJson);

            try
            {
                log.Information($"📤 Starting file upload '{fileName}' ({fileBytes.Length} bytes) to {targetUserId}");

                fileSendReadyTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                await playerSocket.SendAsync(headerBytes, WebSocketMessageType.Text, true, CancellationToken.None);
                log.Information("📤 Sent file header, waiting for server ready signal...");

                var completedTask = await Task.WhenAny(fileSendReadyTcs.Task, Task.Delay(10000));
                if (completedTask != fileSendReadyTcs.Task || !fileSendReadyTcs.Task.Result)
                {
                    log.Error("❌ Timeout or failure waiting for server to accept file.");
                    return;
                }

                int totalSent = 0;
                while (totalSent < fileBytes.Length)
                {
                    if (playerSocket.State != WebSocketState.Open)
                        throw new Exception("WebSocket closed during upload");

                    int size = Math.Min(ChunkSize, fileBytes.Length - totalSent);
                    var chunk = new ArraySegment<byte>(fileBytes, totalSent, size);
                    bool endOfMessage = (totalSent + size) == fileBytes.Length;

                    await playerSocket.SendAsync(chunk, WebSocketMessageType.Binary, endOfMessage, CancellationToken.None);

                    totalSent += size;

                    int percent = (int)((totalSent / (double)fileBytes.Length) * 100);
                    OnUploadProgress?.Invoke(percent);

                    await Task.Delay(10);
                }

                var completeMsg = JsonSerializer.Serialize(new { type = "file_send_complete", fileName, targetUserId });
                var completeBytes = Encoding.UTF8.GetBytes(completeMsg);

                if (playerSocket.State != WebSocketState.Open)
                    throw new Exception("WebSocket closed before completion message");

                await playerSocket.SendAsync(completeBytes, WebSocketMessageType.Text, true, CancellationToken.None);
                log.Information("📤 Sent file complete message.");

                log.Information($"✅ Successfully sent file '{fileName}' to {targetUserId}.");
            }
            catch (Exception ex)
            {
                log.Error(ex, "❌ Failed to send file");
            }
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            var buffer = new byte[8192];
            MemoryStream? currentFileStream = null;
            string? currentFileName = null;
            string? currentSender = null;

            try
            {
                while (playerSocket?.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    var result = await playerSocket.ReceiveAsync(buffer, token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        log.Information("ℹ️ Server closed connection.");
                        await playerSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        log.Information($"⬇️ Received TEXT: {msg}");

                        try
                        {
                            var doc = JsonDocument.Parse(msg);
                            var type = doc.RootElement.GetProperty("type").GetString();

                            switch (type)
                            {
                                case "connection_info":
                                    var instanceName = doc.RootElement.GetProperty("message").GetString();
                                    ConnectedInstanceName = instanceName;
                                    log.Information($"🔗 Connected through instance: {instanceName}");

                                    OnInstanceInfoReceived?.Invoke(instanceName); // <-- hier Event feuern
                                    break;


                                case "user_list_update":
                                    UpdateUserList(doc.RootElement.GetProperty("users"));
                                    break;

                                case "file_send_begin":
                                    if (fileSendReadyTcs != null && !fileSendReadyTcs.Task.IsCompleted)
                                    {
                                        fileSendReadyTcs.SetResult(true);
                                    }
                                    currentFileStream = new MemoryStream();
                                    currentFileName = doc.RootElement.GetProperty("fileName").GetString();
                                    currentSender = doc.RootElement.GetProperty("fromUserId").GetString();
                                    log.Information($"⬇️ Receiving file '{currentFileName}' from {currentSender}");
                                    break;

                                case "file_send_complete":
                                    if (currentFileStream != null && currentFileName != null)
                                    {
                                        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), currentFileName);
                                        await File.WriteAllBytesAsync(path, currentFileStream.ToArray());
                                        log.Information($"✅ File received from {currentSender}, saved as {path}");
                                    }
                                    currentFileStream?.Dispose();
                                    currentFileStream = null;
                                    currentFileName = null;
                                    currentSender = null;
                                    break;

                                case "connect_accepted":
                                    ConnectedToUserId = doc.RootElement.GetProperty("withUserId").GetString();
                                    log.Information($"✅ Connection accepted by {ConnectedToUserId}");
                                    OnConnectionStatusChanged?.Invoke(ConnectedToUserId, true);
                                    break;

                                case "connect_rejected":
                                    var rejectedUser = doc.RootElement.GetProperty("withUserId").GetString();
                                    log.Information($"❌ Connection rejected by {rejectedUser}");
                                    ConnectedToUserId = null;
                                    OnConnectionStatusChanged?.Invoke(rejectedUser, false);
                                    break;

                                case "disconnected":
                                    var disconnectedUser = doc.RootElement.GetProperty("userId").GetString();
                                    log.Information($"⛔️ Disconnected from {disconnectedUser}");
                                    if (ConnectedToUserId == disconnectedUser)
                                    {
                                        ConnectedToUserId = null;
                                        OnConnectionStatusChanged?.Invoke(disconnectedUser, false);
                                    }
                                    break;

                                default:
                                    log.Information($"ℹ️ Unhandled message type: {type}");
                                    break;
                            }
                        }
                        catch (JsonException)
                        {
                            await HandleServerMessageAsync(msg);
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary && currentFileStream != null)
                    {
                        currentFileStream.Write(buffer, 0, result.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, "❌ Error in ReceiveLoop");
            }
        }

        private void UpdateUserList(JsonElement usersElement)
        {
            var newList = new Dictionary<string, string>();

            foreach (var userElem in usersElement.EnumerateArray())
            {
                string id = userElem.GetProperty("userId").GetString() ?? "";
                string? name = userElem.TryGetProperty("userName", out var nameElem) ? nameElem.GetString() : null;
                if (!string.IsNullOrEmpty(id) && id != LocalUserId)
                    newList[id] = name ?? id;
            }

            ConnectedUsers = newList;
            OnUserListChanged?.Invoke();
            log.Information($"📋 User list updated, {ConnectedUsers.Count} users available.");
        }

        public async Task ConnectToAsync(string otherUserId)
        {
            if (playerSocket == null || playerSocket.State != WebSocketState.Open)
            {
                log.Warning("⚠️ Not connected to PlayerServer, cannot send connect request.");
                return;
            }

            var msg = new
            {
                type = "connect",
                targetUserId = otherUserId
            };

            string json = JsonSerializer.Serialize(msg);
            var bytes = Encoding.UTF8.GetBytes(json);

            try
            {
                await playerSocket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
                ConnectedToUserId = otherUserId;
                log.Information($"🔗 Connection request sent to {otherUserId}.");
            }
            catch (Exception ex)
            {
                log.Error(ex, "❌ Error sending connect message");
            }
        }

        public async Task DisconnectAsync()
        {
            if (playerSocket == null || playerSocket.State != WebSocketState.Open)
            {
                log.Warning("⚠️ Not connected, cannot disconnect.");
                return;
            }

            var msg = new { type = "disconnect" };
            string json = JsonSerializer.Serialize(msg);
            var bytes = Encoding.UTF8.GetBytes(json);

            try
            {
                // Sende Disconnect Nachricht an Server
                await playerSocket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
                log.Information("⛔️ Sent disconnect message to server.");

                // Schließe WebSocket sauber (Normal Closure)
                await playerSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnect", CancellationToken.None);
                log.Information("✅ WebSocket closed.");

                // Status komplett zurücksetzen
                ConnectedToUserId = null;
                SessionToken = null;
                ConnectedUsers.Clear();
                ConnectedInstanceName = null;
                ConnectedPlayerServerUrl = null;

                // Event feuern, um UI zu informieren
                OnConnectionStatusChanged?.Invoke(null, false);
                OnUserListChanged?.Invoke();
                OnInstanceInfoReceived?.Invoke(null);

                // Socket und CancellationTokenSource bereinigen
                playerSocket.Dispose();
                playerSocket = null;

                cts?.Cancel();
                cts = null;
            }
            catch (Exception ex)
            {
                log.Error(ex, "❌ Error during disconnect.");
            }
        }

        public void Dispose()
        {
            cts?.Cancel();
            playerSocket?.Dispose();
        }

        private static int GetStableHash(string str)
        {
            unchecked
            {
                int hash = 23;
                foreach (var c in str)
                    hash = hash * 31 + c;
                return Math.Abs(hash);
            }
        }
    }
}
