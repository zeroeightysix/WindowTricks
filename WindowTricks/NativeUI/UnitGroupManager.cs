using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace WindowTricks.NativeUI;

public class UnitGroupManager
{
    public readonly Dictionary<nint, UnitGroup> Index = new();
    public readonly List<UnitGroup> Groups = new();

    public unsafe UnitGroup? Get(AtkUnitBase* addon)
    {
        Index.TryGetValue((nint)addon, out var group);
        return group;
    }

    public unsafe UnitGroup GetOrInstantiate(AtkUnitBase* addon)
    {
        var group = Get(addon);
        if (group != null)
            return group;

        var root = UiUtils.FindRoot(addon);

        if (Index.TryGetValue((nint)root, out group))
        {
            group.Attach(addon);
            Index[(nint)addon] = group;
            return group;
        }

        group = new UnitGroup(root);
        group.Attach(addon);
        Groups.Add(group);
        Index[(nint)root] = group;
        Index[(nint)addon] = group;
        return group;
    }

    public unsafe void Register(AtkUnitBase* addon) => GetOrInstantiate(addon);

    /// <summary>
    /// Remove an addon from the manager.
    /// If this addon was a root node, the entire group disappears.
    /// </summary>
    /// <param name="addon"></param>
    public unsafe void Unregister(AtkUnitBase* addon)
    {
        if (Index.TryGetValue((nint)addon, out var group))
        {
            group.Detach(addon);
            if (group.Root == addon)
            {
                Groups.Remove(group);
                foreach (var u in group.Units) Index.Remove((nint)u.Value);
            }
        }

        Index.Remove((nint)addon);
    }
}
