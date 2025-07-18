using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Moonshare_Plugin
{
    public class UserSessionManager : IDisposable
    {
        private ClientWebSocket? socket;
        private CancellationTokenSource? cts;

        private readonly IDalamudPluginInterface pluginInterface;
        private readonly IPluginLog log;

        public string? LocalUserId { get; private set; }
        public string? ConnectedToUserId { get; private set; }
        public bool IsConnected => socket?.State == WebSocketState.Open;

        public UserSessionManager(IDalamudPluginInterface pluginInterface, IPluginLog log)
        {
            this.pluginInterface = pluginInterface;
            this.log = log;
        }

        public async Task InitializeAsync()
        {
            cts?.Cancel();
            cts = new CancellationTokenSource();
            socket?.Dispose();
            socket = new ClientWebSocket();

            try
            {
                var serverUri = new Uri("ws://localhost:5000/ws");
                log.Information("🌐 Starte Verbindung zum Server...");
                await socket.ConnectAsync(serverUri, cts.Token);
                log.Information("✅ Verbunden mit Moonshare-Server.");

                // Hier starten wir den ReceiveLoop und warten darauf, aber in eigenem Task
                _ = Task.Run(ReceiveLoop, cts.Token);
            }
            catch (Exception ex)
            {
                log.Error($"❌ Verbindung fehlgeschlagen: {ex}");
                // Socket ggf. säubern
                socket.Dispose();
                socket = null;
                cts.Cancel();
            }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[4096];

            try
            {
                while (socket?.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
                {
                    var result = await socket.ReceiveAsync(buffer, cts.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        log.Information("❌ Server hat Verbindung geschlossen.");
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", CancellationToken.None);
                        break;
                    }

                    var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
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
                    catch (Exception e)
                    {
                        log.Warning($"⚠️ Fehler beim Parsen der Nachricht: {e}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                log.Information("ℹ️ ReceiveLoop wurde abgebrochen.");
            }
            catch (Exception e)
            {
                log.Warning($"❌ Fehler im ReceiveLoop: {e}");
            }
            finally
            {
                log.Information("🔌 ReceiveLoop beendet.");
            }
        }

        public async Task ConnectToAsync(string otherUserId)
        {
            if (!IsConnected)
            {
                log.Warning("⚠️ Nicht mit Server verbunden.");
                return;
            }

            var msg = new
            {
                type = "connect",
                targetUserId = otherUserId
            };

            var json = JsonSerializer.Serialize(msg);
            var bytes = Encoding.UTF8.GetBytes(json);

            try
            {
                await socket!.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
                ConnectedToUserId = otherUserId;
                log.Information($"🔗 Verbindung zu {otherUserId} wird versucht...");
            }
            catch (Exception ex)
            {
                log.Error($"❌ Fehler beim Senden der Verbindungsnachricht: {ex}");
            }
        }

        public async Task DisconnectAsync()
        {
            if (!IsConnected) return;

            var msg = new { type = "disconnect" };
            var json = JsonSerializer.Serialize(msg);
            var bytes = Encoding.UTF8.GetBytes(json);

            try
            {
                await socket!.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
                ConnectedToUserId = null;
                log.Information("⛔️ Verbindung getrennt.");
            }
            catch (Exception ex)
            {
                log.Error($"❌ Fehler beim Senden der Trennnachricht: {ex}");
            }
        }

        public void Dispose()
        {
            cts?.Cancel();
            socket?.Dispose();
        }
    }
}
