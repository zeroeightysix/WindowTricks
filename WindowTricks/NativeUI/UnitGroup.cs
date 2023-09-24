using System.Collections.Generic;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace WindowTricks.NativeUI;

public class UnitGroup
{
    public string AddonName { get; }
    public bool Focused => FocusCount > 0;

    internal int FocusCount = 0;

    internal readonly unsafe AtkUnitBase* Root;

    // luckily, we don't care about the hierarchy of the group (although it is usually not more than 1 level tall either way)
    internal readonly HashSet<Pointer<AtkUnitBase>> Units;

    public unsafe UnitGroup(AtkUnitBase* root)
    {
        this.Root = root;
        AddonName = MemoryHelper.ReadStringNullTerminated((nint)root->Name);
        Units = new HashSet<Pointer<AtkUnitBase>> { root };
    }

    public void Attach(Pointer<AtkUnitBase> unit) => Units.Add(unit);
    public void Detach(Pointer<AtkUnitBase> unit) => Units.Remove(unit);
}
