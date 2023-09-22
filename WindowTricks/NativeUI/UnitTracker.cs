using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace WindowTricks.NativeUI;

public abstract class UnitTracker
{
    // This looks ugly. And it is, but we've pinky promised that we'll only access data behind these pointers if we're
    // sure it's valid. I mean, this entire abstraction is to keep the state that way. Together, we can form a beautiful
    // palace of pointers to memory that hasn't been freed yet. Together, we can do anything!
    protected List<Pointer<AtkUnitBase>> unitBases = new();

    private readonly uint target;

    protected UnitTracker(uint target)
    {
        this.target = target;
    }

    protected abstract unsafe void OnNew(AtkUnitBase* newUnitBase);
    protected abstract unsafe void OnDelete(AtkUnitBase* deletedUnitBase);

    public unsafe void OnUpdate()
    {
        var stage = AtkStage.GetSingleton();
        var unitManagers = &stage->RaptureAtkUnitManager->AtkUnitManager;
        var depthFive = &(&unitManagers->DepthLayerOneList)[target];

        // our mission? figure out who came, and who went.
        // units that have disappeared are supposed to be cleaned up and removed from the managed entities (UnitGroup)s
        // units that are new are put into their respective UnitGroups, or create a new one if required.
        List<Pointer<AtkUnitBase>> newUnitBases = new();

        // TODO: can't we basically memcpy this list
        foreach (var index in Enumerable.Range(0, (int)depthFive->Count))
        {
            var addon = (&depthFive->AtkUnitEntries)[index];
            if (RaptureAtkModule.Instance()->AtkModule.IsAddonReady(addon->ID))
                newUnitBases.Add(addon);
        }

        foreach (var unitBase in newUnitBases)
        {
            if (!unitBases.Remove(unitBase))
                OnNew(unitBase); // `unitBase` wasn't in the old list, so it's new
        }

        // all the currently existing AtkUnitBases are removed from the list now, so only the ones that existed last update,
        // but not this one, remain. `unitBases` is now a list of possibly invalid pointers to AtkUnitBase that no longer exist / have moved

        foreach (var deletedUnitBase in unitBases)
            OnDelete(deletedUnitBase);

        // finally, we update the 'previous' unitBases list.
        unitBases = newUnitBases;
    }
}

public class UnitGroupTracker : UnitTracker
{
    // this dictionary is pointer-indexed (wow)
    protected internal Dictionary<nint, UnitGroup> Groups { get; } = new();

    public UnitGroupTracker(uint target) : base(target) { }

    protected override unsafe void OnNew(AtkUnitBase* unitBase)
    {
        // figure out the root of unitBase:
        var parent = UiUtils.FindRoot(unitBase);

        if (Groups.TryGetValue((nint)parent, out var parentGroup))
        {
            parentGroup.Attach(unitBase);
            PluginLog.Debug($"{parentGroup.AddonName}: Attach {(IntPtr)unitBase:X}");
        }
        else
        {
            // create a new group for this root
            var group = CreateGroupForRoot(parent);
            // and add the new child to the group immediately
            group.Attach(unitBase);
            PluginLog.Debug($"Create {group.AddonName} with {(IntPtr)unitBase:X}, root {(IntPtr)parent:X}");
        }
    }

    internal unsafe UnitGroup CreateGroupForRoot(AtkUnitBase* root)
    {
        var group = new UnitGroup(root);
        Groups.Add((nint)root, group);
        return group;
    }

    protected override unsafe void OnDelete(AtkUnitBase* deletedUnitBase)
    {
        var key = (nint)deletedUnitBase;
        if (Groups.TryGetValue(key, out var invalidGroup) && Groups.Remove(key))
            PluginLog.Debug($"{invalidGroup.AddonName}: Group removed " +
                            $"(root {(IntPtr)invalidGroup.root:X}) because of {(nint)deletedUnitBase:X}");

        foreach (var group in Groups.Values)
        {
            if (group.Detach(deletedUnitBase))
                PluginLog.Debug($"{group.AddonName}: Detached {(IntPtr)deletedUnitBase:X})");
        }
    }
}

public class FocusTracker : UnitTracker
{
    public FocusTracker() : base(14) //AtkUnitManager.FocusedUnitsList
    { }

    public bool IsFocused(UnitGroup group) => group.units.Any(unit => unitBases.Contains(unit));

    protected override unsafe void OnNew(AtkUnitBase* newUnitBase) { }

    protected override unsafe void OnDelete(AtkUnitBase* deletedUnitBase) { }
}
