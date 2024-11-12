using System.Collections.Generic;

[System.Serializable]
public class SerializableCaseData
{
    public string ID;
    public string Name;
    public float Price;
    public List<SerializableItemData> Items;

    public SerializableCaseData(CaseData @case)
    {
        ID = @case.ID;
        Name = @case.Name;
        Price = @case.Price;
        Items = new List<SerializableItemData>();

        foreach (var item in @case.Items)
        {
            Items.Add(new SerializableItemData(item));
        }
    }
}