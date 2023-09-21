using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Logging;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace WindowTricks;

public static class NativeUI
{
    // Some sub-windows don't have a `ParentID`. We store the known ones here.
    private static Dictionary<string, string> HiddenParents = new Dictionary<string, string>
    {
        { "FreeCompanyTopics", "FreeCompany" },
        { "FreeCompanyMember", "FreeCompany" },
        { "FreeCompanyRank", "FreeCompany" },
        { "FreeCompanyRights", "FreeCompany" },
        { "FreeCompanyAction", "FreeCompany" },
        { "FreeCompanyActivity", "FreeCompany" },
        { "FreeCompanyStatus", "FreeCompany" },
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
        var rapture = AtkStage.GetSingleton()->RaptureAtkUnitManager;

        while (true)
        {
            consumer?.Invoke(unitBase);
            var parentId = unitBase->ParentID;
            if (parentId == 0)
            {
                var name = MemoryHelper.ReadStringNullTerminated((IntPtr)unitBase->Name);
                if (HiddenParents.TryGetValue(name, out var parent))
                    unitBase = rapture->GetAddonByName(parent);
                else return unitBase;
            }
            else unitBase = rapture->GetAddonById(parentId);
        }
    }
}
