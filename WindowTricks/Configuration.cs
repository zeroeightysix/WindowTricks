using System;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace WindowTricks;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool EnableTransparentWindows { get; set; } = true;
    public byte FocusOpacity { get; set; } = 255;
    public byte UnfocusOpacity { get; set; } = 140;

    // the below exist just to make saving less cumbersome
    [NonSerialized]
    private DalamudPluginInterface? pluginInterface;

    public void Initialize(DalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
    }

    public void Save()
    {
        this.pluginInterface!.SavePluginConfig(this);
    }
}
