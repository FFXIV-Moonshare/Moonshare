using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;
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

        public string? LocalUserId { get; private set; }
        public string? SessionToken { get; private set; }
        public string? ConnectedToUserId { get; private set; }

        public bool IsConnected => playerSocket?.State == WebSocketState.Open;

        public UserSessionManager(IPluginLog log)
        {
            this.log = log;
        }

        public async Task InitializeAsync()
        {
            try
            {
                string userId = "moonshare_user"; // Hier kannst du die UserID dynamisch laden, z.B. aus Config

                // Auth via HTTP GET /sessions?userId=...
                var authToken = await GetAuthTokenFromHttpAsync(userId);
                if (authToken == null)
                {
                    log.Error("❌ Authentication failed: no token received.");
                    return;
                }

                LocalUserId = userId;
                SessionToken = authToken;
                log.Information($"✅ Authenticated as {userId} with token: {authToken}");

                // Connect to PlayerServer via WebSocket
                cts?.Cancel();
                cts = new CancellationTokenSource();
                playerSocket?.Dispose();
                playerSocket = new ClientWebSocket();

                var playerUri = new Uri("ws://localhost:5002/player");
                log.Information("🌐 Connecting to PlayerServer...");
                await playerSocket.ConnectAsync(playerUri, cts.Token);
                log.Information("✅ Connected to PlayerServer.");

                // Send session token to PlayerServer
                string authMsg = JsonSerializer.Serialize(new
                {
                    type = "session_auth",
                    userId = LocalUserId,
                    token = SessionToken
                });
                var authBytes = Encoding.UTF8.GetBytes(authMsg);
                await playerSocket.SendAsync(authBytes, WebSocketMessageType.Text, true, cts.Token);

                // Start receive loop (optional, to handle server messages)
                _ = Task.Run(() => ReceiveLoop(cts.Token), cts.Token);
            }
            catch (Exception ex)
            {
                log.Error(ex, "❌ Error during InitializeAsync");
            }
        }

        private async Task<string?> GetAuthTokenFromHttpAsync(string userId)
        {
            try
            {
                using var httpClient = new HttpClient();
                // Richtig: Port 5003, da dort der HTTP-Server läuft
                var response = await httpClient.GetAsync($"http://localhost:5003/sessions?userId={userId}");

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

        private async Task ReceiveLoop(CancellationToken token)
        {
            var buffer = new byte[4096];
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

                    var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    log.Information($"⬇️ Received from PlayerServer: {msg}");

                    // Hier kannst du noch Nachrichten parsen und verarbeiten
                }
            }
            catch (OperationCanceledException)
            {
                log.Information("ℹ️ ReceiveLoop cancelled.");
            }
            catch (Exception ex)
            {
                log.Error(ex, "❌ Error in ReceiveLoop");
            }
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
                await playerSocket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
                ConnectedToUserId = null;
                log.Information("⛔️ Disconnected.");
            }
            catch (Exception ex)
            {
                log.Error(ex, "❌ Error sending disconnect message");
            }
        }

        public void Dispose()
        {
            cts?.Cancel();
            playerSocket?.Dispose();
        }
    }
}
