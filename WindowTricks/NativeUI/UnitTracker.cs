using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Logging;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace WindowTricks.NativeUI;

public class UnitTracker
{
    // This looks ugly. And it is, but we've pinky promised that we'll only access data behind these pointers if we're
    // sure it's valid. I mean, this entire abstraction is to keep the state that way. Together, we can form a beautiful
    // palace of pointers to memory that hasn't been freed yet. Together, we can do anything!
    private List<Pointer<AtkUnitBase>> unitBases = new();
    
    // this dictionary is pointer-indexed (wow)
    public readonly Dictionary<nint, UnitGroup> groups = new();

    public unsafe void OnUpdate()
    {
        var stage = AtkStage.GetSingleton();
        var unitManagers = &stage->RaptureAtkUnitManager->AtkUnitManager;
        // we're only interested in depth layer 5. that's where the good stuff is
        var depthFive = &unitManagers->DepthLayerFiveList;

        // our mission? figure out who came, and who went.
        // units that have disappeared are supposed to be cleaned up and removed from the managed entities (UnitGroup)s
        // units that are new are put into their respective UnitGroups, or create a new one if required.
        List<Pointer<AtkUnitBase>> newUnitBases = new();
        
        foreach (var index in Enumerable.Range(0, (int)depthFive->Count))
            newUnitBases.Add((&depthFive->AtkUnitEntries)[index]);

        foreach (var unitBase in newUnitBases)
        {
            if (unitBases.Remove(unitBase))
            {
                // `unitBase` was present last update: and it's here now, so we don't care about this node.
            }
            else
            {
                // what duh hell this unitBase is new
                // figure out who its daddy is:
                var daddy = UiUtils.FindRoot(unitBase);

                if (groups.TryGetValue((nint)daddy, out var daddysGroup))
                    daddysGroup.Attach(unitBase);
                else
                {
                    PluginLog.Debug($"create {MemoryHelper.ReadStringNullTerminated((IntPtr)daddy->Name)}");
                    // create a new group
                    var group = new UnitGroup(daddy);
                    group.Attach(unitBase);
                    groups.Add((nint)daddy, group);
                }
            }
        }

        // all the currently existing AtkUnitBases are removed from the list now, so only the ones that existed last update,
        // but not this one, remain. Yuck! those pointers are probably invalid. so we clean them up.

        foreach (var deletedUnitBase in unitBases)
        {
            var key = (nint)deletedUnitBase.Value;
            if (groups.TryGetValue(key, out var invalidGroup))
            {
                groups.Remove(key);
                
                PluginLog.Debug($"Removed group of {invalidGroup.AddonName}");
            }
            else
            {
                foreach (var group in groups.Values)
                {
                    group.Detach(deletedUnitBase);
                }
            }
        }
        
        // finally, we update the 'previous' unitBases list.
        unitBases = newUnitBases;
    }
}
