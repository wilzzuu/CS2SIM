[System.Serializable]
public class SerializableItemData
{
    public string ID;
    public string Name;
    public string Rarity;
    public float BasePrice;
    public float Price;
    public int DemandScore;
    public string Color;
    public string Type;
    public string Style;

    public SerializableItemData(ItemData item)
    {
        ID = item.ID;
        Name = item.Name;
        Rarity = item.Rarity;
        BasePrice = item.BasePrice;
        Price = item.Price;
        DemandScore = item.DemandScore;
        Color = item.Color;
        Type = item.Item;
        Style = item.Style;
    }
}