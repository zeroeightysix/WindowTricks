using System;
using System.Collections.Generic;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace WindowTricks.NativeUI;

public class UnitGroup
{
    public string AddonName { get; }

    internal readonly unsafe AtkUnitBase* root;

    // luckily, we don't care about the hierarchy of the group (although it is usually not more than 1 level tall either way)
    internal readonly List<Pointer<AtkUnitBase>> units;

    public unsafe UnitGroup(AtkUnitBase* root)
    {
        this.root = root;
        AddonName = MemoryHelper.ReadStringNullTerminated((nint)root->Name);
        units = new List<Pointer<AtkUnitBase>> { root };
    }

    public void Attach(Pointer<AtkUnitBase> unit)
    {
        unsafe
        {
            // from the C# spec,
            // comparing numbers may not be used in a safe context.*
            // seriously, who designed this language? microsoft? 
        
        
            // yeah that makes sense i guess
        
            // * by numbers i mean pointers, which are, you guessed it, numbers
            if (unit != root)
                units.Add(unit);
        }
    }

    public bool Detach(Pointer<AtkUnitBase> unit)
    {
        return units.Remove(unit);
    }
}
