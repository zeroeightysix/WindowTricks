using System;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace WindowTricks.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;

    public ConfigWindow(Plugin plugin) : base(
        plugin.Name,
        ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var configValue = this.configuration.EnableTransparentWindows;
        if (ImGui.Checkbox("Enable Transparent Windows", ref configValue))
        {
            configuration.EnableTransparentWindows = configValue;
            configuration.Save();
        }

        if (configuration.EnableTransparentWindows)
        {
            var transparency = (int)configuration.Transparency;
            if (ImGui.SliderInt("Transparency", ref transparency, 20, 254,
                                $"{(int)(transparency / 2.55f)}%%", ImGuiSliderFlags.NoInput))
            {
                configuration.Transparency = (byte)transparency;
                configuration.Save();
            }
        }
    }
}
