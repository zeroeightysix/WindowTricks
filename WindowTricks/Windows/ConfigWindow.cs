using System;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using WindowTricks.NativeUI;

namespace WindowTricks.Windows;

public class ConfigWindow : Window, IDisposable
{
    public UnitGroupTracker UnitTracker { get; }
    private readonly Configuration configuration;

    public ConfigWindow(Plugin plugin, UnitGroupTracker unitTracker) : base(
        plugin.Name,
        ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse)
    {
        UnitTracker = unitTracker;
        this.configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var configValue = this.configuration.EnableTransparentWindows;
        if (ImGui.Checkbox("Enable Transparent Windows", ref configValue))
        {
            configuration.EnableTransparentWindows = configValue;
            UiUtils.ResetAlphas();
            configuration.Save();
        }

        if (configuration.EnableTransparentWindows)
        {
            var focusOpacity = (int)configuration.FocusOpacity;
            if (ImGui.SliderInt("Focused opacity", ref focusOpacity, 20, 255,
                                $"{(int)(focusOpacity / 2.55f)}%%", ImGuiSliderFlags.NoInput))
            {
                configuration.FocusOpacity = (byte)focusOpacity;
                configuration.Save();
            }
            
            var unfocusOpacity = (int)configuration.UnfocusOpacity;
            if (ImGui.SliderInt("Unfocused opacity", ref unfocusOpacity, 20, 254,
                                $"{(int)(unfocusOpacity / 2.55f)}%%", ImGuiSliderFlags.NoInput))
            {
                configuration.UnfocusOpacity = (byte)unfocusOpacity;
                configuration.Save();
            }
        }
        
        ImGui.Text($"{UnitTracker.Groups.Count} groups:");
        foreach (var group in UnitTracker.Groups.Values)
        {
            if (group.Focused) ImGui.PushStyleColor(ImGuiCol.Text, 0xAA00FF00);
            if (ImGui.CollapsingHeader($"{group.AddonName} of {group.units.Count} units"))
            {
                foreach (var unit in group.units)
                {
                    unsafe
                    {
                        ImGui.Text(((IntPtr)unit.Value).ToString("X"));
                    }
                }
            }
            
            if (group.Focused) ImGui.PopStyleColor(1);
        }
    }
}
