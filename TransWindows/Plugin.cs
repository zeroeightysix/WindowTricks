using System.Collections.Generic;
using System.Linq;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Component.GUI;
using TransWindows.Windows;

namespace TransWindows;

public class Service
{
    [PluginService] public static Framework Framework { get; set; } = null!;
}

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "TransWindows";
    private const string CommandName = "/twindows";

    private DalamudPluginInterface PluginInterface { get; init; }
    private CommandManager CommandManager { get; init; }
    public Configuration Configuration { get; init; }
    public WindowSystem WindowSystem = new("TransWindows");

    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    public Plugin(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] CommandManager commandManager)
    {
        this.PluginInterface = pluginInterface;
        pluginInterface.Create<Service>();
        this.CommandManager = commandManager;

        this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        this.Configuration.Initialize(this.PluginInterface);

        // you might normally want to embed resources and load them from the manifest stream

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        this.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });

        this.PluginInterface.UiBuilder.Draw += DrawUI;
        this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        Service.Framework.Update += OnUpdate;
    }

    private void OnUpdate(Framework framework)
    {
        unsafe
        {
            var stage = AtkStage.GetSingleton();
            var unitManagers = &stage->RaptureAtkUnitManager->AtkUnitManager;
            var depthFive = &unitManagers->DepthLayerFiveList;
            var focusedList = &unitManagers->FocusedUnitsList;
            
            var focused = new List<nint>();
            foreach (var index in Enumerable.Range(0, (int)focusedList->Count))
                focused.Add((nint)(&focusedList->AtkUnitEntries)[index]);

            foreach (var index in Enumerable.Range(0, (int)depthFive->Count))
            {
                var unitBase = (&depthFive->AtkUnitEntries)[index];
                if (!unitBase->IsVisible || *unitBase->Name == '_') continue;
                
                unitBase->SetAlpha(focused.Contains((nint)unitBase) ? (byte)255 : (byte)125);
            }
        }
    }

    public void Dispose()
    {
        this.WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        this.CommandManager.RemoveHandler(CommandName);
        
        Service.Framework.Update -= OnUpdate;
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just display our main ui
        MainWindow.IsOpen = true;
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
