using System;
using System.Collections.Generic;

[Serializable]
public class RarityOrder
{
    public static readonly Dictionary<string, int> RarityOrderList = new Dictionary<string, int>
    {
        {"MIL_SPEC", 1},
        {"RESTRICTED", 2},
        {"CLASSIFIED", 3},
        {"COVERT", 4},
        {"SPECIAL", 5}
    };
}