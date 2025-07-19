using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ImGuiNET;
using Moonshare_Plugin.Windows;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

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
    public MainWindow MainWindow { get; init; }
    public CreditsWindow CreditsWindow { get; init; }

    private readonly Action loginHandler;

    public Plugin()
    {
        var stopwatch = Stopwatch.StartNew();

        Log.Information("Initializing Moonshare Plugin...");

        try
        {
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Session = new UserSessionManager(Log);

            var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

            ConfigWindow = new ConfigWindow(this);
            MainWindow = new MainWindow(this, goatImagePath);
            CreditsWindow = new CreditsWindow();

            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);
            WindowSystem.AddWindow(CreditsWindow);

            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the Moonshare window."
            });

            loginHandler = () => _ = OnLoginAsync();
            ClientState.Login += loginHandler;

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.Draw += DrawMainMenu;
            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
            PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

            stopwatch.Stop();
            Log.Information("Moonshare Plugin initialized successfully in {Time} ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize Moonshare Plugin.");
            throw;
        }
    }

    private async Task OnLoginAsync()
    {
        Log.Information("[Moonshare] Attempting to connect to WebSocket server...");

        try
        {
            await Session.InitializeAsync();

            if (Session.IsConnected)
            {
                Log.Information("[Moonshare] Successfully connected. User ID: {UserId}", Session.LocalUserId);
                MainWindow.IsOpen = true;
            }
            else
            {
                Log.Warning("[Moonshare] Connection failed. Is the server reachable?");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[Moonshare] Error while connecting to WebSocket server.");
        }

        ClientState.Login -= loginHandler;
    }

    public void Dispose()
    {
        Log.Information("Unloading Moonshare Plugin...");

        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();
        CreditsWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);

        PluginInterface.UiBuilder.Draw -= DrawUI;
        PluginInterface.UiBuilder.Draw -= DrawMainMenu;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUI;

        ClientState.Login -= loginHandler;

        Log.Information("Moonshare Plugin unloaded.");
    }

    private void OnCommand(string command, string args)
    {
        Log.Debug("Command executed: {Command} {Args}", command, args);
        ToggleMainUI();
    }

    private void DrawUI()
    {
        WindowSystem.Draw();
    }

    private void DrawMainMenu()
    {
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("📁 Moonshare"))
            {
                if (ImGui.MenuItem("📂 Open Window"))
                {
                    Log.Debug("Main menu: Open Window clicked.");
                    ToggleMainUI();
                }

                if (ImGui.MenuItem("⚙️ Configuration"))
                {
                    Log.Debug("Main menu: Configuration clicked.");
                    ToggleConfigUI();
                }

                if (ImGui.MenuItem("🙏 Credits"))
                {
                    Log.Debug("Main menu: Credits clicked.");
                    ToggleCreditsUI();
                }

                ImGui.EndMenu();
            }
            ImGui.EndMainMenuBar();
        }
    }

    public void ToggleConfigUI()
    {
        Log.Debug("Toggling config window.");
        ConfigWindow.Toggle();
    }

    public void ToggleMainUI()
    {
        Log.Debug("Toggling main window.");
        MainWindow.Toggle();
    }

    public void ToggleCreditsUI()
    {
        Log.Debug("Toggling credits window.");
        CreditsWindow.Toggle();
    }
}
