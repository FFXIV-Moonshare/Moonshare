using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WebSocketSharp;

namespace Moonshare_Plugin
{
    public class UserSessionManager : IDisposable
    {
        private WebSocket? socket;
        private readonly IDalamudPluginInterface pluginInterface;
        private readonly IPluginLog log;

        public string? LocalUserId { get; private set; }
        public string? SessionToken { get; private set; }
        public string? ConnectedToUserId { get; private set; }

        public bool IsConnected => socket?.IsAlive == true;

        public UserSessionManager(IDalamudPluginInterface pluginInterface, IPluginLog log)
        {
            this.pluginInterface = pluginInterface;
            this.log = log;
        }

        public async Task InitializeAsync()
        {
            await AuthenticateAndConnect();
        }

        private async Task AuthenticateAndConnect()
        {
            try
            {
                string userId = "moonshare_user";

                var authToken = await GetAuthTokenAsync(userId);
                if (authToken == null)
                {
                    log.Error("❌ Authentifizierung fehlgeschlagen. Kein Token erhalten.");
                    return;
                }

                this.LocalUserId = userId;
                this.SessionToken = authToken;
                log.Information($"✅ Authentifiziert als {userId} mit Token: {authToken}");
            }
            catch (Exception ex)
            {
                log.Error($"❌ Fehler bei Authentifizierung: {ex}");
                return;
            }

            try
            {
                socket = new WebSocket("ws://localhost:5000/ws");

                socket.OnOpen += (sender, e) =>
                {
                    log.Information("✅ Verbunden mit Moonshare-Server.");

                    var authMsg = new
                    {
                        type = "register",
                        userId = LocalUserId,
                        token = SessionToken
                    };
                    string json = JsonSerializer.Serialize(authMsg);
                    socket.Send(json);
                };

                socket.OnMessage += (sender, e) =>
                {
                    var msg = e.Data;
                    log.Information($"⬇️ Empfangen: {msg}");

                    try
                    {
                        var doc = JsonDocument.Parse(msg);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("type", out var typeElem))
                        {
                            var type = typeElem.GetString();

                            switch (type)
                            {
                                case "register":
                                    LocalUserId = root.GetProperty("userId").GetString();
                                    log.Information($"✅ Registriert als UserId = {LocalUserId}");
                                    break;

                                case "message":
                                    var fromId = root.GetProperty("fromUserId").GetString();
                                    var payload = root.GetProperty("payload").GetString();
                                    log.Information($"📩 Nachricht von {fromId}: {payload}");
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Warning($"⚠️ Fehler beim Parsen der Nachricht: {ex}");
                    }
                };

                socket.OnError += (sender, e) =>
                {
                    log.Error($"❌ WebSocket Fehler: {e.Message}");
                };

                socket.OnClose += (sender, e) =>
                {
                    log.Information($"ℹ️ Verbindung geschlossen: {e.Reason}");
                };

                socket.Connect();
            }
            catch (Exception ex)
            {
                log.Error($"❌ Verbindung fehlgeschlagen: {ex}");
            }
        }

        private async Task<string?> GetAuthTokenAsync(string userId)
        {
            using var authSocket = new System.Net.WebSockets.ClientWebSocket();
            var authUri = new Uri("ws://localhost:5001/auth");

            try
            {
                await authSocket.ConnectAsync(authUri, System.Threading.CancellationToken.None);
                var buffer = Encoding.UTF8.GetBytes(userId);
                await authSocket.SendAsync(buffer, System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);

                var receiveBuffer = new byte[1024];
                var result = await authSocket.ReceiveAsync(receiveBuffer, System.Threading.CancellationToken.None);
                var response = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);

                if (response.StartsWith("AUTH_SUCCESS:"))
                    return response["AUTH_SUCCESS:".Length..];
                else
                    return null;
            }
            catch (Exception ex)
            {
                log.Error($"❌ Fehler bei Verbindung zum AuthServer: {ex}");
                return null;
            }
        }

        public async Task ConnectToAsync(string otherUserId)
        {
            if (socket == null || !socket.IsAlive)
            {
                log.Warning("⚠️ Nicht verbunden, kann nicht verbinden.");
                return;
            }

            var msg = new
            {
                type = "connect",
                targetUserId = otherUserId
            };

            string json = JsonSerializer.Serialize(msg);
            await Task.Run(() => socket.Send(json));
            ConnectedToUserId = otherUserId;
            log.Information($"🔗 Verbindungsversuch zu {otherUserId} gesendet.");
        }

        public async Task DisconnectAsync()
        {
            if (socket == null || !socket.IsAlive)
            {
                log.Warning("⚠️ Nicht verbunden, kann nicht trennen.");
                return;
            }

            var msg = new { type = "disconnect" };
            string json = JsonSerializer.Serialize(msg);

            await Task.Run(() => socket.Send(json));
            ConnectedToUserId = null;
            log.Information("⛔️ Verbindung getrennt.");
        }

        public void Dispose()
        {
            if (socket != null)
            {
                if (socket.IsAlive)
                    socket.Close();
                socket = null;
            }
        }
    }
}
