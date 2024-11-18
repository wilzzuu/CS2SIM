using System;
using System.Collections.Generic;

[Serializable]
public class RarityWeights
{
    public static readonly Dictionary<string, float> WeightList = new Dictionary<string, float>
    {
        {"MIL_SPEC", 0.7992f},
        {"RESTRICTED", 0.1598f},
        {"CLASSIFIED", 0.032f},
        {"COVERT", 0.0064f},
        {"SPECIAL", 0.0026f}
    };
}
