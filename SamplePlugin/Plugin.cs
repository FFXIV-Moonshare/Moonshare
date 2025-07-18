using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ImGuiNET;
using Moonshare_Plugin.Windows;
using System.IO;

namespace Moonshare_Plugin;

public sealed class Plugin : IDalamudPlugin
{
    #region Dalamud Services

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    #endregion

    private const string CommandName = "/moonshare";

    public Configuration Configuration { get; init; }
    public UserSessionManager Session { get; private set; }

    public readonly WindowSystem WindowSystem = new("Moonshare");

    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        Session = new UserSessionManager();

        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, goatImagePath);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Zeigt das Moonshare-Pluginfenster an."
        });

        // Login-Event abonnieren (parameterloser Delegate)
        ClientState.Login += OnLogin;

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.Draw += DrawMainMenu;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

        Log.Information("=== Moonshare Plugin gestartet ===");
    }

    private void OnLogin()
    {
        Session.Initialize();
        ClientState.Login -= OnLogin; // einmalig ausführen
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);

        PluginInterface.UiBuilder.Draw -= DrawUI;
        PluginInterface.UiBuilder.Draw -= DrawMainMenu;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUI;

        // Event sauber abmelden falls Dispose vor Login
        ClientState.Login -= OnLogin;

        Log.Information("=== Moonshare Plugin entladen ===");
    }

    private void OnCommand(string command, string args)
    {
        ToggleMainUI();
    }

    private void DrawUI() => WindowSystem.Draw();

    private void DrawMainMenu()
    {
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("📁 Moonshare"))
            {
                if (ImGui.MenuItem("📂 Fenster öffnen"))
                    ToggleMainUI();

                if (ImGui.MenuItem("⚙️ Konfiguration"))
                    ToggleConfigUI();

                ImGui.EndMenu();
            }
            ImGui.EndMainMenuBar();
        }
    }

    public void ToggleConfigUI() => ConfigWindow.Toggle();

    public void ToggleMainUI() => MainWindow.Toggle();
}
