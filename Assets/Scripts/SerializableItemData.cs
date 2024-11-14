using UnityEngine.Serialization;

[System.Serializable]
public class SerializableItemData
{
    public string id;
    public string gun;
    public string name;
    public float basePrice;
    public float price;
    public string condition;
    public bool isStatTrak;
    public string type;
    public string rarity;
    public float weight;
    public int demandScore;

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
    }
}