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
    private bool _needsSaveAfterAttributeMigration;

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
            if (_needsSaveAfterAttributeMigration)
            {
                SaveInventory();
            }
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
            ItemData itemAsset = Resources.Load<ItemData>(itemPath);
            if (itemAsset != null)
            {
                ItemData item = CreateInventoryCopy(itemAsset, sItem);
                item.isLocked = sItem.isLocked;
                items.Add(item);
            }
        }
        return items;
    }

    private ItemData CreateInventoryCopy(ItemData source, SerializableItemData savedData = null)
    {
        int price = source.price;
        int demandScore = source.demandScore;
        float lastActivityTime = source.lastActivityTime;
        bool isLocked = source.isLocked;

        ItemData copy = Instantiate(source);
        copy.price = price;
        copy.demandScore = demandScore;
        copy.lastActivityTime = lastActivityTime;
        copy.isLocked = isLocked;

        if (savedData != null && !string.IsNullOrEmpty(savedData.condition))
        {
            copy.condition = savedData.condition;
        }

        if (savedData != null && savedData.hasRandomizedAttributes)
        {
            copy.pattern = savedData.pattern;
            copy.wearFloat = savedData.wearFloat;
            copy.condition = savedData.condition;
            copy.hasRandomizedAttributes = true;
            copy.price = savedData.price;
            copy.demandScore = savedData.demandScore;
        }
        else if (!copy.hasRandomizedAttributes)
        {
            copy.RandomizePatternAndFloat();
            copy.price = Mathf.RoundToInt(price * copy.PriceMultiplier);
        }

        return copy;
    }

    public bool HasItem(ItemData item)
    {
        return _inventoryItems.Contains(item);
    }

    public void RemoveItemFromInventory(ItemData item)
    {
        if (item.isLocked)
        {
            Debug.LogWarning("Cannot remove a locked item from the inventory.");
            return;
        }

        if (_inventoryItems.Remove(item))
        {
            SaveInventory();
            OnInventoryValueChanged?.Invoke();
        }
    }

    public void RemoveItemsFromInventory(List<ItemData> items)
    {
        int removedCount = _inventoryItems.RemoveAll(item => !item.isLocked && items.Contains(item));
        if (removedCount > 0)
        {
            SaveInventory();
            OnInventoryValueChanged?.Invoke();
        }
    }

    public void AddItemToInventory(ItemData item)
    {
        if (item == null)
        {
            Debug.LogError("Cannot add a null item to the inventory.", this);
            return;
        }

        _inventoryItems.Add(CreateInventoryCopy(item));
        SaveInventory();
        OnInventoryValueChanged?.Invoke();
    }

    public bool SetItemLocked(ItemData item, bool isLocked)
    {
        if (!_inventoryItems.Contains(item))
        {
            Debug.LogWarning("Cannot change the lock state of an item that is not in the inventory.");
            return false;
        }

        item.isLocked = isLocked;
        SaveInventory();
        return true;
    }

    public void AddRandomItemToInventory()
    {
        string itemPath = "ItemAssets";
        ItemData[] allItems = Resources.LoadAll<ItemData>(itemPath);
        ItemData randomItem = allItems[Random.Range(0, allItems.Length)];
        if (randomItem != null)
        {
            AddItemToInventory(randomItem);
            LoadInventory();
        }
    }

    public List<ItemData> GetAllItems()
    {
        return new List<ItemData>(_inventoryItems);
    }

    public List<ItemData> GetUnlockedItems()
    {
        return _inventoryItems.FindAll(item => !item.isLocked);
    }

    public int CalculateInventoryValue()
    {
        int totalValue = 0;
        foreach (ItemData item in _inventoryItems)
        {
            totalValue += item.price;
        }
        return totalValue;
    }

    public void ClearInventory(bool delete)
    {
        _inventoryItems.Clear();
        OnInventoryValueChanged?.Invoke();

        if (delete)
        {
            string path = Path.Combine(SaveFilePath, SaveFileName);

            if (File.Exists(path))
            {
                File.Delete(path);
            }
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
            foreach (SerializableItemData item in data.items)
            {
                if (!item.hasRandomizedAttributes)
                {
                    _needsSaveAfterAttributeMigration = true;
                    break;
                }
            }

            return ConvertSerializableItemsToItemData(data.items);
        }

        return new List<ItemData>(); // Default empty inventory
    }

    public List<ItemData> GetInventoryItems()
    {
        return _inventoryItems;
    }
}
