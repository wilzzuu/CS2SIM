using System.Collections.Generic;

public class RarityData
{

    public static readonly Dictionary<string, string> Name = new Dictionary<string, string>
    {
        { RarityNames.MilSpec, "Mil-Spec" },
        { RarityNames.Restricted, "Restricted" },
        { RarityNames.Classified, "Classified" },
        { RarityNames.Covert, "Covert" },
        { RarityNames.Special, "Special" }
    };
    public static readonly Dictionary<string, float> Weight = new()
    {
        { "Mil-Spec", 0.7992f },
        { "Restricted", 0.1598f },
        { "Classified", 0.032f },
        { "Covert", 0.0064f },
        { "Special", 0.0026f }
    };

    public static readonly Dictionary<string, int> Order = new()
    {
        { "Mil-Spec", 1 },
        { "Restricted", 2 },
        { "Classified", 3 },
        { "Covert", 4 },
        { "Special", 5 }
    };
}

public static class RarityNames
{
    public const string MilSpec = "Mil-Spec";
    public const string Restricted = "Restricted";
    public const string Classified = "Classified";
    public const string Covert = "Covert";
    public const string Special = "Special";
}