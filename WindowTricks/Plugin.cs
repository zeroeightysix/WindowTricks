using System.Linq;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using WindowTricks.NativeUI;
using WindowTricks.Windows;

namespace WindowTricks;

public class Service
{
    [PluginService]
    public static Framework Framework { get; set; } = null!;
}

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class Plugin : IDalamudPlugin
{
    public string Name => "WindowTricks";

    private DalamudPluginInterface PluginInterface { get; init; }
    private CommandManager CommandManager { get; init; }
    public Configuration Configuration { get; init; }
    public readonly WindowSystem WindowSystem = new("WindowTricks");
    private ConfigWindow ConfigWindow { get; init; }

    private UnitGroupTracker fiveTracker = new(4);
    private FocusTracker focusTracker;

    // HUD addons that shouldn't be transparent
    private static readonly string[] IgnoredUnits =
    {
        "Hud",
        "AreaMap",
        "ChatLog",
        "ScenarioTree"
    };

    public Plugin(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] CommandManager commandManager)
    {
        pluginInterface.Create<Service>();
        this.PluginInterface = pluginInterface;
        this.CommandManager = commandManager;

        this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        this.Configuration.Initialize(this.PluginInterface);

        ConfigWindow = new ConfigWindow(this, fiveTracker);

        WindowSystem.AddWindow(ConfigWindow);

        this.CommandManager.AddHandler("/wtricks", new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });

        pluginInterface.UiBuilder.Draw += DrawUI;
        pluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        Service.Framework.Update += OnUpdate;

        focusTracker = new FocusTracker(fiveTracker);
    }

    private void OnUpdate(Framework framework)
    {
        if (!Configuration.EnableTransparentWindows)
            return;
        
        unsafe
        {
            fiveTracker.OnUpdate();
            focusTracker.OnUpdate();
            
            foreach (var group in fiveTracker.Groups.Values)
            {
                if (IgnoredUnits.Contains(group.AddonName))
                    continue;
                foreach (var unit in group.units)
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
