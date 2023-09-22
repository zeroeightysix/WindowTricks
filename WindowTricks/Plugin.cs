using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Memory;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Component.GUI;
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

    private UnitTracker unitTracker = new();

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

        ConfigWindow = new ConfigWindow(this, unitTracker);

        WindowSystem.AddWindow(ConfigWindow);

        this.CommandManager.AddHandler("/wtricks", new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });

        pluginInterface.UiBuilder.Draw += DrawUI;
        pluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        Service.Framework.Update += OnUpdate;
    }

    private void OnUpdate(Framework framework)
    {
        unsafe
        {
            unitTracker.OnUpdate();
            
            var stage = AtkStage.GetSingleton();
            var unitManagers = &stage->RaptureAtkUnitManager->AtkUnitManager;
            var depthFive = &unitManagers->DepthLayerFiveList;
            var focusedList = &unitManagers->FocusedUnitsList;

            var focused = new List<nint>();
            foreach (var index in Enumerable.Range(0, (int)focusedList->Count))
            {
                var unitBase = (&focusedList->AtkUnitEntries)[index];
                var root = UiUtils.FindRoot(unitBase);
                focused.Add((nint)root);
            }

            foreach (var index in Enumerable.Range(0, (int)depthFive->Count))
            {
                var unitBase = UiUtils.FindRoot((&depthFive->AtkUnitEntries)[index]);

                if (!unitBase->IsVisible) continue;
                if (*unitBase->Name == '_' || IgnoredUnits.Contains(MemoryHelper.ReadStringNullTerminated((IntPtr)unitBase->Name)))
                    continue;
                
                unitBase->SetAlpha(focused.Contains((nint)unitBase) ? (byte)255 : Configuration.Transparency);
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
