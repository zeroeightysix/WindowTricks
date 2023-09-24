using System;
using Dalamud.Interface.Windowing;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using WindowTricks.NativeUI;

namespace WindowTricks.Windows;

public class ConfigWindow : Window, IDisposable
{
    public UnitGroupManager UnitGroupManager { get; }
    private readonly Configuration configuration;

    public ConfigWindow(Plugin plugin, UnitGroupManager unitGroupManager) : base(
        plugin.Name,
        ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse)
    {
        UnitGroupManager = unitGroupManager;
        this.configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override unsafe void Draw()
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
        
        foreach (var group in UnitGroupManager.Groups)
        {
            if (group.Focused) ImGui.PushStyleColor(ImGuiCol.Text, 0xFF00FF00);
            if (ImGui.TreeNode($"{group.AddonName} ({group.FocusCount})###{(nint)group.Root}"))
            {
                foreach (var unit in group.Units)
                {
                    var addon = unit.Value;
                    ImGui.Text($"{MemoryHelper.ReadStringNullTerminated((nint)addon->Name)} at {(nint)addon:X}");
                }

                ImGui.TreePop();
            }
            if (group.Focused) ImGui.PopStyleColor(1);
        }
    }
}
