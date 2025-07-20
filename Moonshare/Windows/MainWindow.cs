using Dalamud.Interface.Windowing;
using ImGuiNET;
using Moonshare_Plugin;
using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    private bool isConnecting = false;

    private List<string> availableFiles = new();
    private int selectedFileIndex = -1;
    private string selectedFileName => (selectedFileIndex >= 0 && selectedFileIndex < availableFiles.Count) ? availableFiles[selectedFileIndex] : "";

    private int selectedUserIndex = -1;

    private const string filesFolder = @"C:\Users\Public";

    private string connectInput = "";

    public MainWindow(Plugin plugin) : base("📁 Moonshare Connection")
    {
        this.plugin = plugin;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(400, 300),
            MaximumSize = new Vector2(800, 600)
        };

        LoadAvailableFiles();

        plugin.Session.OnUserListChanged += () => IsOpen = true;
    }

    private void LoadAvailableFiles()
    {
        availableFiles.Clear();
        if (Directory.Exists(filesFolder))
        {
            try { availableFiles.AddRange(Directory.GetFiles(filesFolder)); }
            catch (Exception ex) { Plugin.Log.Error($"Error loading files: {ex}"); }
        }
        else Plugin.Log.Warning($"Folder does not exist: {filesFolder}");
    }

    public void Dispose()
    {
        // Keine besonderen Ressourcen
    }

    public override void Draw()
    {
        ImGui.Text("Status: ");

        if (plugin.Session.IsConnected)
            ImGui.TextColored(new Vector4(0, 1, 0, 1), $"✅ Connected as {plugin.Session.LocalUserId}");
        else
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "❌ Not connected");

        ImGui.Separator();

        if (plugin.Session.IsConnected)
        {
            ImGui.Text("🧑‍🤝‍🧑 Available users:");

            var users = plugin.Session.ConnectedUsers;

            if (users.Count == 0)
            {
                ImGui.Text("No other users online.");
            }
            else
            {
                ImGui.BeginChild("UserListChild", new Vector2(0, 120), true);

                int i = 0;
                foreach (var kvp in users)
                {
                    bool isSelected = (i == selectedUserIndex);
                    string displayName = $"{kvp.Value} ({kvp.Key})";

                    if (ImGui.Selectable(displayName, isSelected))
                        selectedUserIndex = i;

                    if (ImGui.IsItemClicked() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        _ = plugin.Session.ConnectToAsync(kvp.Key);

                    i++;
                }

                ImGui.EndChild();
            }
        }
        else
        {
            ImGui.Text("Enter UserID to connect:");

            if (ImGui.InputText("##connectInput", ref connectInput, 64))
            {
                if (ImGui.IsKeyPressed(ImGuiKey.Enter) && !string.IsNullOrWhiteSpace(connectInput))
                    _ = ConnectAsync(connectInput.Trim());
            }

            if (isConnecting)
            {
                ImGui.BeginDisabled();
                ImGui.Button("Connect");
                ImGui.EndDisabled();
                ImGui.Text("Connecting...");
            }
            else
            {
                if (ImGui.Button("Connect"))
                {
                    if (!string.IsNullOrWhiteSpace(connectInput))
                        _ = ConnectAsync(connectInput.Trim());
                    else
                        Plugin.Log.Warning("⚠️ Please enter a valid UserID.");
                }
            }
        }

        ImGui.Separator();

        if (plugin.Session.IsConnected)
        {
            if (ImGui.CollapsingHeader("📁 Send File"))
            {
                ImGui.Text($"Files in folder: {filesFolder}");

                ImGui.BeginChild("FileListChild", new Vector2(0, 150), true);

                for (int i = 0; i < availableFiles.Count; i++)
                {
                    string fileNameOnly = Path.GetFileName(availableFiles[i]);
                    bool isSelected = (i == selectedFileIndex);
                    if (ImGui.Selectable(fileNameOnly, isSelected))
                        selectedFileIndex = i;
                }

                ImGui.EndChild();

                // Upload-Fortschritt anzeigen, nur wenn gerade Upload läuft
                if (plugin.isUploading)
                {
                    ImGui.Text($"Uploading: {plugin.uploadProgress}%");
                    ImGui.ProgressBar(plugin.uploadProgress / 100f, new Vector2(-1, 0));
                }

                if (!string.IsNullOrEmpty(selectedFileName))
                {
                    ImGui.Text($"Selected file: {Path.GetFileName(selectedFileName)}");
                    if (ImGui.Button("Send File"))
                    {
                        if (!string.IsNullOrEmpty(plugin.Session.ConnectedToUserId))
                        {
                            try
                            {
                                byte[] fileBytes = File.ReadAllBytes(selectedFileName);
                                _ = plugin.Session.SendFileAsync(plugin.Session.ConnectedToUserId, fileBytes, Path.GetFileName(selectedFileName));
                            }
                            catch (Exception ex)
                            {
                                Plugin.Log.Error($"Error reading file: {ex}");
                            }
                        }
                        else Plugin.Log.Warning("⚠️ You are not connected to any user.");
                    }
                }
                else ImGui.Text("No file selected.");
            }

            if (ImGui.Button("Disconnect"))
                _ = plugin.Session.DisconnectAsync();
        }
    }

    private async Task ConnectAsync(string userId)
    {
        try
        {
            isConnecting = true;
            await plugin.Session.InitializeAsync();

            if (plugin.Session.IsConnected)
                await plugin.Session.ConnectToAsync(userId);
            else
                Plugin.Log.Warning("⚠️ Could not connect to the server.");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"❌ Error during connection: {ex}");
        }
        finally
        {
            isConnecting = false;
        }
    }
}
