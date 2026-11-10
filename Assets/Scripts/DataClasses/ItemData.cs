using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "New Item Data", order = 51)]
public class ItemData : ScriptableObject
{
    public const int MinimumPattern = 0;
    public const int MaximumPattern = 1000;

    private const float FactoryNewMaxFloat = 0.07f;
    private const float MinimalWearMaxFloat = 0.15f;
    private const float FieldTestedMaxFloat = 0.38f;
    private const float WellWornMaxFloat = 0.45f;

    public static readonly HashSet<int> SpecialPatterns = new HashSet<int>();
    public static readonly Dictionary<int, float> SpecialPatternPriceMultipliers = new Dictionary<int, float>();

    public string id;
    public string gun;
    public new string name;
    public int basePrice;
    public int price;
    public string condition;
    public int pattern = -1;
    public float wearFloat = -1f;
    public bool hasRandomizedAttributes;
    public bool isStatTrak;
    public string type;
    public string rarity;
    public float weight;
    public int demandScore;
    public string imageName;
    public bool isLocked;

    public float lastActivityTime;
    public const float DemandDecayInterval = 120f;
    public const float DecayRate = 0.1f;

    public bool IsSpecialPattern =>
        SpecialPatterns.Contains(pattern) || SpecialPatternPriceMultipliers.ContainsKey(pattern);

    public float WearPriceMultiplier
    {
        get
        {
            if (!hasRandomizedAttributes) return 1f;
            if (wearFloat < FactoryNewMaxFloat) return 1.5f;
            if (wearFloat < MinimalWearMaxFloat) return 1.2f;
            if (wearFloat < FieldTestedMaxFloat) return 1f;
            if (wearFloat < WellWornMaxFloat) return 0.85f;
            return 0.7f;
        }
    }

    public float PatternPriceMultiplier =>
        SpecialPatternPriceMultipliers.TryGetValue(pattern, out float multiplier) ? multiplier : 1f;

    public float PriceMultiplier => WearPriceMultiplier * PatternPriceMultiplier;

    public int ModifiedBasePrice => Mathf.RoundToInt(basePrice * PriceMultiplier);

    public void RandomizePatternAndFloat()
    {
        pattern = Random.Range(MinimumPattern, MaximumPattern + 1);
        if (!TryGetWearFloatRange(condition, out float minimumFloat, out float maximumFloat))
        {
            Debug.LogError($"Cannot assign a wear float for unknown item condition '{condition}'.", this);
            wearFloat = Random.Range(0f, 1f);
        }
        else
        {
            wearFloat = Random.Range(minimumFloat, maximumFloat - 0.000001f);
        }

        hasRandomizedAttributes = true;
    }

    private static bool TryGetWearFloatRange(string itemCondition, out float minimumFloat, out float maximumFloat)
    {
        switch (itemCondition)
        {
            case "Factory New":
                minimumFloat = 0f;
                maximumFloat = FactoryNewMaxFloat;
                return true;
            case "Minimal Wear":
                minimumFloat = FactoryNewMaxFloat;
                maximumFloat = MinimalWearMaxFloat;
                return true;
            case "Field-Tested":
                minimumFloat = MinimalWearMaxFloat;
                maximumFloat = FieldTestedMaxFloat;
                return true;
            case "Well-Worn":
                minimumFloat = FieldTestedMaxFloat;
                maximumFloat = WellWornMaxFloat;
                return true;
            case "Battle-Scarred":
                minimumFloat = WellWornMaxFloat;
                maximumFloat = 1f;
                return true;
            default:
                minimumFloat = 0f;
                maximumFloat = 1f;
                return false;
        }
    }

    private void OnEnable()
    {
        demandScore = 0;
        price = basePrice;
        lastActivityTime = Time.time;
    }
}
