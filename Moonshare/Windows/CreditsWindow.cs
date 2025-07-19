using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;

namespace Moonshare_Plugin.Windows;

public class CreditsWindow : Window, IDisposable
{
    public CreditsWindow() : base(
        "🌙 Moonshare Credits",
        ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)
    {
    }

    public void Dispose() { }

    public override void Draw()
    {
        ImGui.TextWrapped("Special thanks to everyone who contributed to Moonshare!");
        ImGui.Separator();

        ImGui.BulletText("Ambiente - Core Plugin Development");
        ImGui.BulletText("Jan - Networking Feedback & Testing");
        ImGui.BulletText("Lupi - Design");
        ImGui.BulletText("Clara - Idea and Brainstorming");
        ImGui.BulletText("Freddy - Management & Discord");
        ImGui.BulletText("Dalamud Team - API and Framework");
        ImGui.BulletText("ImGui.NET - UI Rendering");

        ImGui.Spacing();
        ImGui.Text("Made with ❤Love♥ for the FFXIV modding community.");
    }
}
