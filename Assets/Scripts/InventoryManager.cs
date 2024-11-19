using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class InventoryData
{
    public List<SerializableItemData> items;
}

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    private List<ItemData> _inventoryItems = new List<ItemData>();

    private string SaveFilePath
    {
        get
        {

            #if UNITY_EDITOR
                return Path.Combine(Application.persistentDataPath, "EditorData");
            #else
                return Path.Combine(Application.persistentDataPath, "SaveData");
            #endif
        }
    }

    private const string SaveFileName = "InventoryData.save";

    public delegate void InventoryValueChangedHandler();
    public event InventoryValueChangedHandler OnInventoryValueChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            _inventoryItems = LoadInventory();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private List<ItemData> ConvertSerializableItemsToItemData(List<SerializableItemData> serializableItems)
    {
        List<ItemData> items = new List<ItemData>();
        foreach (SerializableItemData sItem in serializableItems)
        {
            string itemPath = $"ItemAssets/{sItem.id}";
            ItemData item = Resources.Load<ItemData>(itemPath);
            if (item != null)
            {
                items.Add(item);
            }
            else
            {
                Debug.LogWarning($"Failed to load ItemData at {itemPath}. Asset with ID {sItem.id} may be missing or incorrectly named.");
            }
        }
        return items;
    }

    public bool HasItem(ItemData item)
    {
        return _inventoryItems.Contains(item);
    }

    public void RemoveItemFromInventory(ItemData item)
    {
        if (_inventoryItems.Remove(item))
        {
            SaveInventory();
            OnInventoryValueChanged?.Invoke();
        }
    }

    public void AddItemToInventory(ItemData item)
    {
        _inventoryItems.Add(item);
        SaveInventory();
        OnInventoryValueChanged?.Invoke();
    }

    public List<ItemData> GetAllItems()
    {
        return new List<ItemData>(_inventoryItems);
    }

    public float CalculateInventoryValue()
    {
        float totalValue = 0;
        foreach (ItemData item in _inventoryItems)
        {
            totalValue += item.price;
        }
        return totalValue * 0.85f;
    }

    public void ClearInventory()
    {
        _inventoryItems.Clear();
        string path = Path.Combine(SaveFilePath, SaveFileName);

        if (File.Exists(path))
        {
            File.Delete(path);
        }
        
        SaveInventory();
    }

    private void SaveInventory()
    {
        InventoryData data = new InventoryData
        {
            items = _inventoryItems.ConvertAll(item => new SerializableItemData(item))
        };

        string path = Path.Combine(SaveFilePath, SaveFileName);
        string jsonData = JsonUtility.ToJson(data);
        string encryptedData = DataEncryptionUtility.Encrypt(jsonData);

        File.WriteAllText(path, encryptedData);
    }
    
    private List<ItemData> LoadInventory()
    {
        string path = Path.Combine(SaveFilePath, SaveFileName);

        if (File.Exists(path))
        {
            string encryptedData = File.ReadAllText(path);
            string jsonData = DataEncryptionUtility.Decrypt(encryptedData);

            InventoryData data = JsonUtility.FromJson<InventoryData>(jsonData);
            return ConvertSerializableItemsToItemData(data.items);
        }

        return new List<ItemData>(); // Default empty inventory
    }

    public List<ItemData> GetInventoryItems()
    {
        return _inventoryItems;
    }
}
