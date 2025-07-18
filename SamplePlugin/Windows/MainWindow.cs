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
    private bool isConnecting = false; // Damit man den Button während Verbindungsversuch deaktiviert

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
                ImGui.Text("Verbindung wird hergestellt...");
                ImGui.BeginDisabled();
                ImGui.Button("Verbinden");
                ImGui.EndDisabled();
            }
            else
            {
                if (ImGui.Button("Verbinden"))
                {
                    if (!string.IsNullOrWhiteSpace(connectInput))
                    {
                        _ = ConnectAsync(connectInput.Trim());
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
