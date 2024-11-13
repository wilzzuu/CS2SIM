using UnityEngine.Serialization;

[System.Serializable]
public class SerializableItemData
{
    public string ID { get; }
    public string Gun { get; }
    public string Name { get; }
    public float BasePrice { get; }
    public float Price { get; }
    public string Condition { get; }
    public bool IsStatTrak { get; }
    public string Type { get; }
    public string Rarity { get; }
    public int Weight { get; }
    public int DemandScore { get; }

    public SerializableItemData(ItemData item)
    {
        ID = item.id;
        Gun = item.gun;
        Name = item.name;
        BasePrice = item.basePrice;
        Price = item.price;
        Condition = item.condition;
        IsStatTrak = item.isStatTrak;
        Type = item.type;
        Rarity = item.rarity;
        Weight = item.weight;
        DemandScore = item.demandScore;
    }
}