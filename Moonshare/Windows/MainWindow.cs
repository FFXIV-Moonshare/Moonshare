using Dalamud.Interface.Windowing;
using ImGuiNET;
using Moonshare_Plugin;
using System;
using System.Numerics; // Für Vector4
using System.Threading.Tasks;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    private string connectInput = ""; // UserId, zu der verbunden werden soll
    private bool isConnecting = false; // Button deaktivieren während Verbindungsversuch

    public MainWindow(Plugin plugin, string goatImagePath) : base("📁 Moonshare Verbindung")
    {
        this.plugin = plugin;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(400, 180),
            MaximumSize = new Vector2(800, 600)
        };
    }

    public void Dispose()
    {
        // Hier ggf. Ressourcen freigeben, falls nötig
    }

    public override void Draw()
    {
        ImGui.Text("Status: ");

        if (plugin.Session.IsConnected)
        {
            ImGui.TextColored(new Vector4(0, 1, 0, 1), $"✅ Verbunden als {plugin.Session.LocalUserId}");
        }
        else
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "❌ Nicht verbunden");
        }

        ImGui.Separator();
        ImGui.Text("UserID zum Verbinden eingeben:");

        ImGui.InputText("##connectInput", ref connectInput, 64);

        if (!plugin.Session.IsConnected)
        {
            if (isConnecting)
            {
                ImGui.BeginDisabled();
                ImGui.Button("Verbinden");
                ImGui.EndDisabled();
                ImGui.Text("Verbindung wird hergestellt...");
            }
            else
            {
                if (ImGui.Button("Verbinden"))
                {
                    if (!string.IsNullOrWhiteSpace(connectInput))
                    {
                        _ = ConnectAsync(connectInput.Trim());
                    }
                    else
                    {
                        Plugin.Log.Warning("⚠️ Bitte eine gültige UserID eingeben.");
                    }
                }
            }
        }
        else
        {
            if (ImGui.Button("Verbindung trennen"))
            {
                _ = plugin.Session.DisconnectAsync();
            }
        }
    }

    private async Task ConnectAsync(string userId)
    {
        try
        {
            isConnecting = true;
            await plugin.Session.InitializeAsync();

            if (plugin.Session.IsConnected)
            {
                await plugin.Session.ConnectToAsync(userId);
            }
            else
            {
                Plugin.Log.Warning("⚠️ Verbindung zum Server konnte nicht hergestellt werden.");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"❌ Fehler beim Verbindungsaufbau: {ex}");
        }
        finally
        {
            isConnecting = false;
        }
    }
}
