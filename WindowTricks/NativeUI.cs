using System;
using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace WindowTricks;

public static class NativeUI
{
    // Some sub-windows don't have a `ParentID`. We store the known ones here.
    private static Dictionary<string, string> HiddenParents = new Dictionary<string, string>
    {
        { "FreeCompanyMember", "FreeCompany" }
    };

    public static unsafe void ResetAlphas()
    {
        var stage = AtkStage.GetSingleton();
        var five = &stage->RaptureAtkUnitManager->AtkUnitManager.DepthLayerFiveList;
        foreach (var index in Enumerable.Range(0, (int)five->Count))
        {
            (&five->AtkUnitEntries)[index]->SetAlpha(255);
        }
    }
    
    public static unsafe AtkUnitBase* FollowUp(AtkUnitBase* unitBase, Action<Pointer<AtkUnitBase>>? consumer = null)
    {
        while (true)
        {
            consumer?.Invoke(unitBase);
            var parentId = unitBase->ParentID;
            if (parentId == 0) return unitBase;
            unitBase = AtkStage.GetSingleton()->RaptureAtkUnitManager->GetAddonById(parentId);
        }
    }
}
