using System;
using System.Collections.Generic;

public class ConditionData
{
    public static readonly Dictionary<Condition, string> Name = new Dictionary<Condition, string>
    {
        { Condition.BattleScarred, "Battle-Scarred" },
        { Condition.WellWorn, "Well-Worn" },
        { Condition.FieldTested, "Field-Tested" },
        { Condition.MinimalWear, "Minimal Wear" },
        { Condition.FactoryNew, "Factory New" }
    };

    public static readonly Dictionary<string, string> ShortName = new Dictionary<string, string>
    {
        { "Battle-Scarred", "BS" },
        { "Well-Worn", "WW" },
        { "Field-Tested", "FT" },
        { "Minimal Wear", "MW" },
        { "Factory New", "FN" }
    };

    public static readonly Dictionary<string, int> Order = new Dictionary<string, int>
    {
        { Name[Condition.BattleScarred], 1 },
        { Name[Condition.WellWorn], 2 },
        { Name[Condition.FieldTested], 3 },
        { Name[Condition.MinimalWear], 4 },
        { Name[Condition.FactoryNew], 5 }
    };

    public enum Condition
    {
        BattleScarred,
        WellWorn,
        FieldTested,
        MinimalWear,
        FactoryNew
    }
}