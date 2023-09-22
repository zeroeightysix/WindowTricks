using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Logging;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace WindowTricks.NativeUI;

public abstract class UnitTracker
{
    // This looks ugly. And it is, but we've pinky promised that we'll only access data behind these pointers if we're
    // sure it's valid. I mean, this entire abstraction is to keep the state that way. Together, we can form a beautiful
    // palace of pointers to memory that hasn't been freed yet. Together, we can do anything!
    private List<Pointer<AtkUnitBase>> unitBases = new();

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
            newUnitBases.Add((&depthFive->AtkUnitEntries)[index]);

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
        // what duh hell this unitBase is new
        // figure out who its daddy is:
        var daddy = UiUtils.FindRoot(unitBase);

        if (Groups.TryGetValue((nint)daddy, out var daddysGroup))
            daddysGroup.Attach(unitBase);
        else
        {
            PluginLog.Debug($"create {MemoryHelper.ReadStringNullTerminated((IntPtr)daddy->Name)}");
            // create a new group
            CreateGroupForRoot(unitBase, daddy);
        }
    }

    internal unsafe UnitGroup CreateGroupForRoot(AtkUnitBase* unitBase, AtkUnitBase* root)
    {
        var group = new UnitGroup(root);
        if (unitBase != root)
            group.Attach(unitBase);
        Groups.Add((nint)root, group);
        return group;
    }

    protected override unsafe void OnDelete(AtkUnitBase* deletedUnitBase)
    {
        var key = (nint)deletedUnitBase;
        if (Groups.TryGetValue(key, out var invalidGroup))
        {
            Groups.Remove(key);

            PluginLog.Debug($"Removed group of {invalidGroup.AddonName}");
        }
        else
        {
            foreach (var group in Groups.Values)
            {
                group.Detach(deletedUnitBase);
            }
        }
    }
}

public class FocusTracker : UnitTracker
{
    private readonly UnitGroupTracker groupTracker;

    public FocusTracker(UnitGroupTracker groupTracker) : base(14) //AtkUnitManager.FocusedUnitsList
    {
        this.groupTracker = groupTracker;
    }

    protected override unsafe void OnNew(AtkUnitBase* unitBase)
    {
        var root = UiUtils.FindRoot(unitBase);
        if (!groupTracker.Groups.TryGetValue((nint)root, out var group))
            group = groupTracker.CreateGroupForRoot(unitBase, root);

        group.Focused = true;
    }

    protected override unsafe void OnDelete(AtkUnitBase* unitBase)
    {
        // we cannot rely on unitBase being a valid pointer, or it remembering its parent, so we have to go look for it
        foreach (var group in groupTracker.Groups.Values)
        {
            if (group.children.Contains(unitBase)) group.Focused = false;
        }
    }
}
