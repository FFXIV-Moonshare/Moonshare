using System;
using System.Numerics;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ImGuiNET;
using System.IO;

public class CreditsWindow : Window
{
    private ISharedImmediateTexture? moonImage;
    private readonly ITextureProvider textureProvider;
    private readonly IPluginLog log;
    private IntPtr moonImageHandle = IntPtr.Zero;

    private float scrollOffset = 0f;
    private double lastTime = 0;

    public CreditsWindow(ITextureProvider textureProvider, IPluginLog log)
        : base("🌙 Moonshare Credits", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.textureProvider = textureProvider;
        this.log = log;

        LoadImage();
    }

    private void LoadImage()
    {
        var moonPath = @"C:\Users\rober\Source\Repos\Moonshare\Moonshare\resources\moon3.png";

        try
        {
            if (File.Exists(moonPath))
            {
                moonImage = textureProvider.GetFromFile(moonPath);
                if (moonImage != null)
                {
                    var resourceProperty = moonImage.GetType().GetProperty("Resource");
                    if (resourceProperty != null)
                    {
                        var resource = resourceProperty.GetValue(moonImage);
                        if (resource != null)
                        {
                            var nativePointerProperty = resource.GetType().GetProperty("NativePointer");
                            if (nativePointerProperty != null)
                            {
                                moonImageHandle = (IntPtr)nativePointerProperty.GetValue(resource)!;
                            }
                        }
                    }
                }
            }
            else
            {
                log.Warning($"Moonshare logo not found at path: {moonPath}");
            }
        }
        catch (Exception ex)
        {
            log.Error($"Failed to load moon logo: {ex}");
        }
    }

    public override void Draw()
    {
        // Simples Scroll-Timing
        double currentTime = ImGui.GetTime();
        float delta = (float)(currentTime - lastTime);
        lastTime = currentTime;

        scrollOffset += delta * 30f; // Geschwindigkeit (30 px pro Sekunde)

        ImGui.BeginChild("ScrollingRegion", new Vector2(400, 400), true, ImGuiWindowFlags.HorizontalScrollbar);

        ImGui.SetCursorPosY(400 - scrollOffset); // Start unten, bewege nach oben

        if (moonImageHandle != IntPtr.Zero)
        {
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - 128) / 2);
            ImGui.Image(moonImageHandle, new Vector2(128, 128));
            ImGui.Spacing();
        }

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
        ImGui.Text("Made with ♥Love♥ for the FFXIV modding community.");

        ImGui.EndChild();

        // Reset wenn Credits ganz durchgelaufen sind (optional)
        if (scrollOffset > 600)
            scrollOffset = 0;
    }
}
