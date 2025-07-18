using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Moonshare_Plugin.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;

    public ConfigWindow(Plugin plugin) : base("Moonshare Einstellungen###MoonshareConfigWindow")
    {
        // Entferne NoResize, damit das Fenster resizable ist
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        // Anfangsgröße (kann frei verändert werden)
        Size = new Vector2(400, 300);

        // Optional: Minimale/maximale Fenstergröße
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(300, 200),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        // Fenster beweglich machen, wenn aktiviert
        if (Configuration.IsConfigWindowMovable)
            Flags &= ~ImGuiWindowFlags.NoMove;
        else
            Flags |= ImGuiWindowFlags.NoMove;
    }

    public override void Draw()
    {
        ImGui.TextWrapped("Hier kannst du die Einstellungen für das Moonshare-Plugin anpassen:");

        ImGui.Separator();
        ImGui.Spacing();

        // Boolean Setting: Fenster beweglich
        var movable = Configuration.IsConfigWindowMovable;
        if (ImGui.Checkbox("Fenster beweglich", ref movable))
        {
            Configuration.IsConfigWindowMovable = movable;
            Configuration.Save();
        }

        // Boolean Setting: Auto Reconnect
        var autoReconnect = Configuration.EnableAutoReconnect;
        if (ImGui.Checkbox("Auto Reconnect aktivieren", ref autoReconnect))
        {
            Configuration.EnableAutoReconnect = autoReconnect;
            Configuration.Save();
        }

        ImGui.Spacing();

        // String Setting: Server Adresse
        var serverAddr = Configuration.ServerAddress;
        if (ImGui.InputText("Server-Adresse", ref serverAddr, 256))
        {
            Configuration.ServerAddress = serverAddr;
            Configuration.Save();
        }

        ImGui.Spacing();

        // Integer Setting: Reconnect Delay in Sekunden
        var reconnectDelay = Configuration.ReconnectDelaySeconds;
        if (ImGui.InputInt("Reconnect Delay (Sekunden)", ref reconnectDelay))
        {
            Configuration.ReconnectDelaySeconds = Math.Max(1, reconnectDelay);
            Configuration.Save();
        }

        ImGui.Spacing();

        // Integer Setting: Max gleichzeitige Transfers
        var maxTransfers = Configuration.MaxConcurrentTransfers;
        if (ImGui.InputInt("Max. gleichzeitige Transfers", ref maxTransfers))
        {
            Configuration.MaxConcurrentTransfers = Math.Clamp(maxTransfers, 1, 10);
            Configuration.Save();
        }

        ImGui.Spacing();
        ImGui.Separator();

        ImGui.TextWrapped("Weitere Einstellungen können hier hinzugefügt werden...");
    }
}
