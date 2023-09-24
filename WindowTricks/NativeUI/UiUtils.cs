using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace WindowTricks.NativeUI;

public static class UiUtils
{
    // Some sub-windows don't have a `ParentID`. We store the known ones here.
    private static Dictionary<string, string> HiddenParents = new()
    {
        { "FreeCompanyTopics", "FreeCompany" },
        { "FreeCompanyMember", "FreeCompany" },
        { "FreeCompanyRank", "FreeCompany" },
        { "FreeCompanyRights", "FreeCompany" },
        { "FreeCompanyAction", "FreeCompany" },
        { "FreeCompanyActivity", "FreeCompany" },
        { "FreeCompanyStatus", "FreeCompany" },
        { "FriendList", "Social" },
        { "PartyMemberList", "Social" },
        { "BlackList", "Social" },
        { "SocialList", "Social" },
    };

    public static unsafe void ResetAlphas()
    {
        var stage = AtkStage.GetSingleton();
        var five = &stage->RaptureAtkUnitManager->AtkUnitManager.DepthLayerFiveList;
        foreach (var ptr in five->EntriesSpan)
        {
            ptr.Value->SetAlpha(255);
        }
    }

    public static unsafe AtkUnitBase* FindRoot(AtkUnitBase* unitBase)
    {
        var rapture = AtkStage.GetSingleton()->RaptureAtkUnitManager;

        while (true)
        {
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
