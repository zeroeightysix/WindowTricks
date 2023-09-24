using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using WindowTricks.NativeUI;
using WindowTricks.Windows;

namespace WindowTricks;

public class Service
{
    [PluginService]
    public static IFramework Framework { get; set; } = null!;

    [PluginService]
    public static IPluginLog Log { get; set; } = null!;
    
    [PluginService]
    public static IAddonLifecycle AddonLifecycle { get; set; } = null!;
}

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class Plugin : IDalamudPlugin
{
    public string Name => "WindowTricks";

    private DalamudPluginInterface PluginInterface { get; init; }
    private ICommandManager CommandManager { get; init; }
    public Configuration Configuration { get; init; }
    public readonly WindowSystem WindowSystem = new("WindowTricks");
    private ConfigWindow ConfigWindow { get; init; }
    private UnitGroupManager UnitGroupManager { get; set; }
    private FocusTracker FocusTracker { get; set; }

    // HUD addons that shouldn't be transparent
    private static readonly string[] IgnoredUnits =
    {
        "Hud",
        "AreaMap",
        "ChatLog",
        "ScenarioTree",
        "SelectYesno"
    };

    public Plugin(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] ICommandManager commandManager)
    {
        pluginInterface.Create<Service>();
        this.PluginInterface = pluginInterface;
        this.CommandManager = commandManager;

        this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        this.Configuration.Initialize(this.PluginInterface);

        UnitGroupManager = new UnitGroupManager();
        FocusTracker = new FocusTracker(UnitGroupManager);
        ConfigWindow = new ConfigWindow(this, UnitGroupManager);

        WindowSystem.AddWindow(ConfigWindow);

        this.CommandManager.AddHandler("/wtricks", new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });

        pluginInterface.UiBuilder.Draw += DrawUI;
        pluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        Service.Framework.Update += OnUpdate;
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "", OnAddonSetup);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "", OnAddonFinalize);
    }

    private unsafe void OnAddonSetup(AddonEvent eventtype, AddonArgs addoninfo)
    {
        UnitGroupManager.Register((AtkUnitBase*)addoninfo.Addon);
    }

    private unsafe void OnAddonFinalize(AddonEvent eventtype, AddonArgs addoninfo)
    {
        UnitGroupManager.Unregister((AtkUnitBase*)addoninfo.Addon);
    }

    private void OnUpdate(IFramework framework)
    {
        if (!Configuration.EnableTransparentWindows)
            return;
        
        FocusTracker.OnUpdate();
        
        foreach (var group in UnitGroupManager.Groups)
        {
            unsafe
            {
                foreach (var unit in group.Units)
                {
                    unit.Value->SetAlpha(group.Focused ? Configuration.FocusOpacity : Configuration.UnfocusOpacity);
                }
            }
        }
    }

    public void Dispose()
    {
        this.WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();

        this.CommandManager.RemoveHandler("/wtricks");

        Service.Framework.Update -= OnUpdate;
    }

    private void OnCommand(string command, string args)
    {
        switch (args)
        {
            case "reset":
                UiUtils.ResetAlphas();
                break;
            default:
                ConfigWindow.IsOpen = !ConfigWindow.IsOpen;
                break;
        }
    }

    private void DrawUI()
    {
        this.WindowSystem.Draw();
    }

    public void DrawConfigUI()
    {
        ConfigWindow.IsOpen = true;
    }
}
