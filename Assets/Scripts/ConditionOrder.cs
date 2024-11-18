using System;
using System.Collections.Generic;

[Serializable]

public class ConditionOrder
{
    public static readonly Dictionary<string, int> ConditionOrderList = new Dictionary<string, int>
    {
        {"Battle-Scarred",1},
        {"Well-Worn",2},
        {"Field-Tested",3},
        {"Minimal Wear",4},
        {"Factory New",5}
    };
}