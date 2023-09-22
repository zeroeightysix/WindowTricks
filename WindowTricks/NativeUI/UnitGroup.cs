using System;
using System.Collections.Generic;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace WindowTricks.NativeUI;

public class UnitGroup
{
    public bool Focused { get; set; } = false;
    public string AddonName { get; }

    private readonly unsafe AtkUnitBase* root;

    // luckily, we don't care about the hierarchy of the group (although it is usually not more than 1 level tall either way)
    internal readonly List<Pointer<AtkUnitBase>> children = new();

    /// <summary>
    /// SAFETY: This object is safe as long as the provided <see cref="AtkUnitBase"/> pointer is valid,
    /// and all children attached through <see cref="Attach"/> are valid pointers.
    /// </summary>
    /// <param name="root"></param>
    public unsafe UnitGroup(AtkUnitBase* root)
    {
        this.root = root;
        AddonName = MemoryHelper.ReadStringNullTerminated((IntPtr)root->Name);
    }

    public ushort GetRootId()
    {
        unsafe
        {
            return root->ID;
        }
    }

    public void Attach(Pointer<AtkUnitBase> child)
    {
        children.Add(child);
    }

    public void Detach(Pointer<AtkUnitBase> child)
    {
        children.Remove(child);
    }
}
