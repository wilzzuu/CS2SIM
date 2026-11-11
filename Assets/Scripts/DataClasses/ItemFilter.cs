using System;
using System.Collections.Generic;
using System.Linq;

public sealed class ItemFilter
{
    public readonly HashSet<string> Conditions = new HashSet<string>(StringComparer.Ordinal);
    public readonly HashSet<string> Rarities = new HashSet<string>(StringComparer.Ordinal);
    public readonly HashSet<string> Types = new HashSet<string>(StringComparer.Ordinal);
    public readonly HashSet<string> Guns = new HashSet<string>(StringComparer.Ordinal);
    public readonly HashSet<bool> StatTrak = new HashSet<bool>();

    public bool Matches(ItemData item)
    {
        return (Conditions.Count == 0 || Conditions.Contains(item.condition))
            && (Rarities.Count == 0 || Rarities.Contains(item.rarity))
            && (Types.Count == 0 || Types.Contains(item.type))
            && (Guns.Count == 0 || Guns.Contains(item.gun))
            && (StatTrak.Count == 0 || StatTrak.Contains(item.isStatTrak));
    }

    public IEnumerable<ItemData> Apply(IEnumerable<ItemData> items)
    {
        return items.Where(Matches);
    }

    public void Clear()
    {
        Conditions.Clear();
        Rarities.Clear();
        Types.Clear();
        Guns.Clear();
        StatTrak.Clear();
    }
}
