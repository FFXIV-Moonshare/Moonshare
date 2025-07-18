using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Common.Math;
using ImGuiNET;
using Moonshare_Plugin;
using System;
using static Dalamud.Interface.Windowing.Window;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly string imagePath;

    private string otherUserInput = "";
    private string localIdBuffer = "";

    public MainWindow(Plugin plugin, string imagePath) : base("📁 FileLinker Verbindung")
    {
        this.plugin = plugin;
        this.imagePath = imagePath;

        localIdBuffer = plugin.Session.LocalUserId ?? "";

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(400, 200),
            MaximumSize = new Vector2(9999, 9999)
        };
    }

    public void UpdateLocalId()
    {
        localIdBuffer = plugin.Session.LocalUserId ?? "";
    }

    public void Dispose() { }

    public override void Draw()
    {
        ImGui.Text("Deine eindeutige UserID:");

        // NICHT localIdBuffer hier überschreiben!
        ImGui.InputText("##localid", ref localIdBuffer, 64, ImGuiInputTextFlags.ReadOnly);

        ImGui.Spacing();
        ImGui.Separator();

        ImGui.Text("Verbindung zu anderem Spieler:");
        ImGui.InputText("UserID eingeben", ref otherUserInput, 64);

        if (ImGui.Button("Verbinden"))
        {
            if (!string.IsNullOrWhiteSpace(otherUserInput))
            {
                plugin.Session.ConnectTo(otherUserInput.Trim());
            }
        }

        if (plugin.Session.IsConnected)
        {
            ImGui.TextColored(new Vector4(0, 1, 0, 1), $"✅ Verbunden mit: {plugin.Session.ConnectedToUserId}");
            if (ImGui.Button("Verbindung trennen"))
            {
                plugin.Session.Disconnect();
            }
        }
        else
        {
            ImGui.TextColored(new Vector4(1, 1, 0, 1), "⚠ Nicht verbunden.");
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Text("📦 Dateiübertragung wird vorbereitet …");
    }
}
