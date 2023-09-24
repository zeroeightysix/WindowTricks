using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace WindowTricks.NativeUI;

public class FocusTracker
{
    public UnitGroupManager UnitGroupManager { get; }
    private List<Pointer<AtkUnitBase>> lastFocus = new();
    
    public FocusTracker(UnitGroupManager unitGroupManager)
    {
        UnitGroupManager = unitGroupManager;
    }

    public unsafe void OnUpdate()
    {
        var focused = &AtkStage.GetSingleton()->RaptureAtkUnitManager->AtkUnitManager.FocusedUnitsList;

        List<Pointer<AtkUnitBase>> newFocus = new();
        
        foreach (var index in Enumerable.Range(0, focused->Count))
        {
            var addon = focused->EntriesSpan[index].Value;
            newFocus.Add(addon);
            
            if (!lastFocus.Remove(addon))
            {
                // Focus has been gained on this addon.
                UnitGroupManager.GetOrInstantiate(addon).FocusCount++;
            }
        }

        foreach (var lostFocus in lastFocus)
        {
            var addon = lostFocus.Value;
            // Focus was lost on this addon.
            // It might also just be gone entirely, so this is nullable.
            var group = UnitGroupManager.Get(addon);
            if (group != null) group.FocusCount--;
        }
        
        lastFocus = newFocus;
    }
    
}
