using System.Collections.Generic;

[System.Serializable]
public class SerializableCaseData
{
    public string ID { get; }
    public string Name { get; }
    public float Price { get; }
    public List<SerializableItemData> items;

    public SerializableCaseData(CaseData @case)
    {
        ID = @case.id;
        Name = @case.name;
        Price = @case.price;
        items = new List<SerializableItemData>();

        foreach (var item in @case.items)
        {
            items.Add(new SerializableItemData(item));
        }
    }
}