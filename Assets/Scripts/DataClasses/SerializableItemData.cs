[System.Serializable]
public class SerializableItemData
{
    public string id;
    public string gun;
    public string name;
    public int basePrice;
    public int price;
    public string condition;
    public bool isStatTrak;
    public string type;
    public string rarity;
    public float weight;
    public int demandScore;
    public bool isLocked;

    public SerializableItemData(ItemData item)
    {
        id = item.id;
        gun = item.gun;
        name = item.name;
        basePrice = item.basePrice;
        price = item.price;
        condition = item.condition;
        isStatTrak = item.isStatTrak;
        type = item.type;
        rarity = item.rarity;
        weight = item.weight;
        demandScore = item.demandScore;
        isLocked = item.isLocked;
    }
}