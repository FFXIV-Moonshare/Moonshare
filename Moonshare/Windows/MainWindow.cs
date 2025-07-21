using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Moonshare_Plugin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private string currentFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    private bool isConnecting = false;
    private bool showLogWindow = false;
    private Stopwatch uploadStopwatch = new();
    private DateTime lastUserListUpdate = DateTime.MinValue;

    private List<string> availableFiles = new();
    private int selectedFileIndex = -1;
    private string selectedFileName => (selectedFileIndex >= 0 && selectedFileIndex < availableFiles.Count) ? availableFiles[selectedFileIndex] : "";

    private int selectedUserIndex = -1;

 
   
    private string connectInput = "";
    private string userFilterInput = "";


    private List<string> logMessages = new();

    private bool autoScrollLog = true;

    public MainWindow(Plugin plugin) : base("Moonshare - File Sharing & Connections")
    {
        this.plugin = plugin;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(700, 500),
            MaximumSize = new Vector2(1200, 900)
        };

        LoadAvailableFiles();

        plugin.Session.OnUserListChanged += () =>
        {
            lastUserListUpdate = DateTime.Now;
            IsOpen = true;
            AddLog("User list updated.");
        };

        plugin.Session.OnUploadProgress += progress =>
        {
            plugin.uploadProgress = progress;
            if (progress == 100)
            {
                uploadStopwatch.Stop();
                AddLog($"Upload finished in {uploadStopwatch.Elapsed.TotalSeconds:F1}s.");
            }
        };
    }

    private void AddLog(string text)
    {
        logMessages.Add($"{DateTime.Now:HH:mm:ss} - {text}");
        if (logMessages.Count > 200) logMessages.RemoveAt(0);
       
    }

    private void LoadAvailableFiles()
    {
        availableFiles.Clear();
        if (Directory.Exists(currentFolder))
        {
            try
            {
                availableFiles.AddRange(Directory.GetFiles(currentFolder));
                AddLog($"Loaded files from: {currentFolder}");
            }
            catch (Exception ex)
            {
                AddLog($"Error loading files: {ex.Message}");
            }
        }
        else
        {
            AddLog($"Folder does not exist: {currentFolder}");
        }
    }

    public void Dispose()
    {
        
    }

    public override void Draw()
    {
        ImGui.PushFont(UiBuilder.IconFont);

        DrawHeader();

        ImGui.PopFont();

        ImGui.BeginChild("MainChild", new Vector2(0, -80), false);

        ImGui.BeginTable("MainColumns", 2, ImGuiTableFlags.None);

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        DrawConnectionPanel();

        ImGui.TableSetColumnIndex(1);
        DrawFilePanel();

        ImGui.EndTable();

        ImGui.EndChild();

        DrawBottomBar();

        DrawLogWindow();
    }

    private void DrawHeader()
    {
        ImGui.TextColored(new Vector4(0.2f, 0.7f, 0.9f, 1f), "Moonshare - Connecting FFXIV Players");
        ImGui.Separator();
        ImGui.Spacing();
    }

    private void DrawConnectionPanel()
    {
        ImGui.BeginChild("ConnectionPanel", new Vector2(0, 0), true);

        ImGui.TextColored(new Vector4(0.4f, 0.9f, 0.6f, 1f), "Connection Details");
        ImGui.Spacing();

        var connected = plugin.Session.IsConnected;

        ImGui.BeginTable("ConnDetailsTable", 2, ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.BordersInnerV);

        ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
        ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.Text("User ID:");
        ImGui.TableSetColumnIndex(1);
        ImGui.TextColored(new Vector4(1f, 1f, 1f, 1f), plugin.Session.LocalUserId);

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.Text("Session Token:");
        ImGui.TableSetColumnIndex(1);
        string token = plugin.Session.SessionToken ?? "(not available)";
        string tokenShort = token.Length > 12 ? token[..12] + "..." : token;
        ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f), tokenShort);
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(token);

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.Text("PlayerServer URL:");
        ImGui.TableSetColumnIndex(1);
        ImGui.TextWrapped(plugin.Session.ConnectedPlayerServerUrl ?? "(not connected)");

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.Text("WebSocket State:");
        ImGui.TableSetColumnIndex(1);
        ImGui.TextColored(connected ? new Vector4(0f, 1f, 0f, 1f) : new Vector4(1f, 0f, 0f, 1f),
            connected ? "Connected" : "Disconnected");

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.Text("Connected as:");
        ImGui.TableSetColumnIndex(1);
        string connectedTo = plugin.Session.ConnectedToUserId ?? "(none)";
        ImGui.TextColored(new Vector4(1f, 1f, 0.4f, 1f), connectedTo);

        ImGui.EndTable();

        ImGui.Spacing();

        if (!connected)
        {
            ImGui.Text("Enter UserID to connect:");
            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("##connectInput", ref connectInput, 64);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Enter the UserID you want to connect to");

            ImGui.Spacing();

            if (isConnecting)
            {
                ImGui.BeginDisabled();
                ImGui.Button("Connecting...");
                ImGui.EndDisabled();
            }
            else
            {
                bool canConnect = !string.IsNullOrWhiteSpace(connectInput);
                if (!canConnect)
                    ImGui.BeginDisabled();

                if (ImGui.Button("Connect"))
                {
                    _ = ConnectAsync(connectInput.Trim());
                }

                if (!canConnect)
                    ImGui.EndDisabled();
            }
        }
        else
        {
            if (ImGui.Button("Disconnect"))
            {
                _ = plugin.Session.DisconnectAsync();
                selectedUserIndex = -1;
            }
        }

        ImGui.Spacing();
        ImGui.Separator();

        ImGui.TextColored(new Vector4(0.4f, 0.9f, 0.6f, 1f), "Available Users");
        ImGui.Text($"Online: {plugin.Session.ConnectedUsers.Count}");

        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("Search users...", ref userFilterInput, 64);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Filter users by name or ID");

        ImGui.BeginChild("UserListChild", new Vector2(0, 300), true);

        var filteredUsers = plugin.Session.ConnectedUsers
            .Where(kvp => kvp.Value.Contains(userFilterInput, StringComparison.OrdinalIgnoreCase) || kvp.Key.Contains(userFilterInput))
            .OrderBy(kvp => kvp.Value)
            .ToList();

        for (int i = 0; i < filteredUsers.Count; i++)
        {
            var kvp = filteredUsers[i];
            bool isSelected = (i == selectedUserIndex);
            string displayName = $"{kvp.Value} ({kvp.Key})";

            if (ImGui.Selectable(displayName, isSelected))
                selectedUserIndex = i;

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip($"UserId: {kvp.Key}");

            if (ImGui.IsItemClicked() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                _ = plugin.Session.ConnectToAsync(kvp.Key);
                AddLog($"Connection request sent to {kvp.Key}");
            }
        }

        ImGui.EndChild();

        ImGui.EndChild();
    }

    private void DrawFilePanel()
    {
        ImGui.BeginChild("FilePanel", new Vector2(0, 0), true);

        ImGui.TextColored(new Vector4(0.7f, 0.8f, 1f, 1f), "📁 Files");
        ImGui.Text("Folder:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(400);
        ImGui.InputText("##folderInput", ref currentFolder, 512);

        ImGui.SameLine();
        if (ImGui.SmallButton("📂 Load"))
        {
            LoadAvailableFiles();
        }

        if (ImGui.InputText("##folderSelect", ref currentFolder, 512, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            LoadAvailableFiles();
        }

        ImGui.SameLine();
        if (ImGui.Button("Load Folder"))
        {
            LoadAvailableFiles();
        }

        ImGui.BeginChild("FileListChild", new Vector2(0, 250), true);

        ImGui.BeginTable("FileListTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg);
        ImGui.TableSetupColumn("Filename", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.WidthFixed, 80);
        ImGui.TableSetupColumn("Modified", ImGuiTableColumnFlags.WidthFixed, 140);
        ImGui.TableHeadersRow();

        for (int i = 0; i < availableFiles.Count; i++)
        {
            var path = availableFiles[i];
            string fileName = Path.GetFileName(path);
            long fileSize = 0;
            DateTime lastMod = DateTime.MinValue;

            try
            {
                var fi = new FileInfo(path);
                fileSize = fi.Length;
                lastMod = fi.LastWriteTime;
            }
            catch { }

            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            bool isSelected = (i == selectedFileIndex);
            if (ImGui.Selectable(fileName, isSelected, ImGuiSelectableFlags.SpanAllColumns))
                selectedFileIndex = i;

            ImGui.TableSetColumnIndex(1);
            ImGui.Text($"{fileSize / 1024} KB");

            ImGui.TableSetColumnIndex(2);
            ImGui.Text(lastMod.ToString("g"));
        }

        ImGui.EndTable();
        ImGui.EndChild();

        ImGui.Spacing();

        if (plugin.isUploading)
        {
            ImGui.TextColored(new Vector4(0.3f, 0.7f, 0.9f, 1f), $"Uploading {plugin.uploadProgress}%");
            ImGui.ProgressBar(plugin.uploadProgress / 100f, new Vector2(-1, 0));
            ImGui.Text($"Elapsed time: {uploadStopwatch.Elapsed.TotalSeconds:F1}s");

            double eta = uploadStopwatch.Elapsed.TotalSeconds / (plugin.uploadProgress / 100.0) * (100 - plugin.uploadProgress);
            if (!double.IsInfinity(eta))
                ImGui.Text($"ETA: {eta:F1}s");
        }
        else if (selectedFileIndex >= 0)
        {
            ImGui.Text($"Selected file: {Path.GetFileName(selectedFileName)}");
            if (plugin.Session.IsConnected && !string.IsNullOrEmpty(plugin.Session.ConnectedToUserId))
            {
                if (ImGui.Button("Send File"))
                {
                    try
                    {
                        byte[] fileBytes = File.ReadAllBytes(selectedFileName);
                        plugin.isUploading = true;
                        uploadStopwatch.Restart();

                        _ = plugin.Session.SendFileAsync(plugin.Session.ConnectedToUserId, fileBytes, Path.GetFileName(selectedFileName))
                            .ContinueWith(_ => plugin.isUploading = false);
                    }
                    catch (Exception ex)
                    {
                        AddLog($"Error reading file: {ex.Message}");
                    }
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Send the selected file to the connected user");
            }
            else
            {
                ImGui.TextColored(new Vector4(1, 0.4f, 0.4f, 1), "Connect to a user first to send files.");
            }
        }
        else
        {
            ImGui.Text("No file selected.");
        }

        ImGui.EndChild();
    }

    private void DrawBottomBar()
    {
        ImGui.Separator();

        if (ImGui.BeginTable("BottomBar", 2, ImGuiTableFlags.None))
        {
            ImGui.TableNextRow(); 

            ImGui.TableSetColumnIndex(0);
            ImGui.Text($"Users Online: {plugin.Session.ConnectedUsers.Count}");

            ImGui.TableSetColumnIndex(1);
            if (ImGui.Button(showLogWindow ? "Hide Log" : "Show Log"))
            {
                showLogWindow = !showLogWindow;
            }

            ImGui.EndTable();
        }
    }

    private void DrawLogWindow()
    {
        if (!showLogWindow)
            return;

        ImGui.SetNextWindowSize(new Vector2(800, 200), ImGuiCond.FirstUseEver);
        ImGui.Begin("Log Window", ref showLogWindow, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.HorizontalScrollbar);

        if (ImGui.SmallButton("Clear Log"))
        {
            logMessages.Clear();
        }
        ImGui.SameLine();

        bool scrollToBottom = ImGui.SmallButton("Scroll to Bottom");
        if (scrollToBottom)
        {
            autoScrollLog = true;
        }

        ImGui.Separator();

        ImGui.BeginChild("LogTextChild", new Vector2(0, 0), false, ImGuiWindowFlags.HorizontalScrollbar);

        foreach (var line in logMessages)
        {
            ImGui.TextWrapped(line);
        }

        if (autoScrollLog)
            ImGui.SetScrollHereY(1.0f);

        ImGui.EndChild();
        ImGui.End();
    }


    private async Task ConnectAsync(string userId)
    {
        try
        {
            isConnecting = true;
            AddLog($"Trying to connect to {userId}...");
            await plugin.Session.InitializeAsync();

            if (plugin.Session.IsConnected)
            {
                await plugin.Session.ConnectToAsync(userId);
                AddLog($"Connected to {userId}");
            }
            else
            {
                AddLog("⚠️ Could not connect to the server.");
            }
        }
        catch (Exception ex)
        {
            AddLog($"❌ Error during connection: {ex.Message}");
        }
        finally
        {
            isConnecting = false;
        }
    }
}
