using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TradeUpManager : MonoBehaviour
{
    public static TradeUpManager Instance { get; private set; }

    private const int REQUIRED_ITEMS = 10;

    private List<ItemData> _selectedItems = new List<ItemData>();
    private string _selectedRarity;
    private bool _selectedStatTrak;

    public delegate void SelectionChangedHandler();
    public event SelectionChangedHandler OnSelectionChanged;

    public delegate void TradeUpCompleteHandler(ItemData resultItem);
    public event TradeUpCompleteHandler OnTradeUpComplete;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Adds an item to the trade-up selection.
    /// Returns false if the item cannot be added (e.g., different rarity or StatTrak status).
    /// </summary>
    public bool AddItemToSelection(ItemData item)
    {
        if (item == null)
        {
            Debug.LogError("TradeUpManager: Cannot add null item to selection.");
            return false;
        }

        if (_selectedItems.Count > 0)
        {
            if (item.rarity != _selectedRarity)
            {
                Debug.LogWarning($"TradeUpManager: Item rarity '{item.rarity}' does not match selection rarity '{_selectedRarity}'.");
                return false;
            }

            if (item.isStatTrak != _selectedStatTrak)
            {
                Debug.LogWarning($"TradeUpManager: Item StatTrak status does not match selection.");
                return false;
            }
        }
        else
        {
            _selectedRarity = item.rarity;
            _selectedStatTrak = item.isStatTrak;
        }

        if (_selectedItems.Count >= REQUIRED_ITEMS)
        {
            Debug.LogWarning($"TradeUpManager: Selection is already at maximum capacity ({REQUIRED_ITEMS} items).");
            return false;
        }

        _selectedItems.Add(item);
        OnSelectionChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Removes an item from the trade-up selection.
    /// </summary>
    public bool RemoveItemFromSelection(ItemData item)
    {
        if (_selectedItems.Remove(item))
        {
            if (_selectedItems.Count == 0)
            {
                _selectedRarity = null;
            }
            OnSelectionChanged?.Invoke();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Clears all selected items.
    /// </summary>
    public void ClearSelection()
    {
        _selectedItems.Clear();
        _selectedRarity = null;
        OnSelectionChanged?.Invoke();
    }

    /// <summary>
    /// Gets the current number of selected items.
    /// </summary>
    public int GetSelectedItemCount()
    {
        return _selectedItems.Count;
    }

    /// <summary>
    /// Gets all currently selected items.
    /// </summary>
    public List<ItemData> GetSelectedItems()
    {
        return new List<ItemData>(_selectedItems);
    }

    /// <summary>
    /// Checks if a selection is valid and ready for trade-up.
    /// </summary>
    public bool IsSelectionValid()
    {
        return _selectedItems.Count == REQUIRED_ITEMS;
    }

    /// <summary>
    /// Executes the trade-up with currently selected items.
    /// Returns the result item on success, null on failure.
    /// </summary>
    public ItemData ExecuteTradeUp()
    {
        if (!IsSelectionValid())
        {
            Debug.LogError($"TradeUpManager: Cannot execute trade-up. Expected {REQUIRED_ITEMS} items, got {_selectedItems.Count}.");
            return null;
        }

        string nextRarity = GetNextRarity(_selectedRarity);
        if (nextRarity == null)
        {
            Debug.LogError($"TradeUpManager: No higher rarity exists for '{_selectedRarity}'.");
            return null;
        }

        string resultCondition = CalculateResultCondition(_selectedItems);

        ItemData resultItem = SelectRandomItemOfRarity(nextRarity, resultCondition);

        if (resultItem != null)
        {
            resultItem.isStatTrak = _selectedStatTrak;
            OnTradeUpComplete?.Invoke(resultItem);
            ClearSelection();
        }
        else
        {
            Debug.LogWarning($"TradeUpManager: Could not find any item with rarity '{nextRarity}' and condition '{resultCondition}'.");
        }

        return resultItem;
    }

    /// <summary>
    /// Gets the next rarity in the progression order.
    /// </summary>
    private string GetNextRarity(string currentRarity)
    {
        if (!RarityData.Order.TryGetValue(currentRarity, out int currentOrder))
        {
            return null;
        }

        int nextOrder = currentOrder + 1;
        var nextRarityEntry = RarityData.Order.FirstOrDefault(x => x.Value == nextOrder);

        return nextRarityEntry.Key != null ? nextRarityEntry.Key : null;
    }

    /// <summary>
    /// Calculates the result condition based on the average condition of selected items.
    /// </summary>
    private string CalculateResultCondition(List<ItemData> items)
    {
        if (items.Count == 0)
        {
            return ConditionData.Name[ConditionData.Condition.FieldTested];
        }

        float averageConditionOrder = 0f;
        foreach (ItemData item in items)
        {
            if (ConditionData.Order.TryGetValue(item.condition, out int conditionOrder))
            {
                averageConditionOrder += conditionOrder;
            }
        }

        averageConditionOrder /= items.Count;

        int resultConditionOrder = Mathf.RoundToInt(averageConditionOrder);
        resultConditionOrder = Mathf.Clamp(resultConditionOrder, 1, 5);

        var resultConditionEntry = ConditionData.Order.FirstOrDefault(x => x.Value == resultConditionOrder);
        return resultConditionEntry.Key ?? ConditionData.Name[ConditionData.Condition.FieldTested];
    }

    /// <summary>
    /// Selects a random item of the specified rarity and condition from the game's item pool.
    /// </summary>
    private ItemData SelectRandomItemOfRarity(string rarity, string condition)
    {
        ItemData[] allItems = Resources.LoadAll<ItemData>("ItemAssets");

        List<ItemData> matchingItems = allItems
            .Where(item => item.rarity == rarity && item.condition == condition)
            .ToList();

        if (matchingItems.Count == 0)
        {
            return null;
        }

        ItemData selectedItem = matchingItems[Random.Range(0, matchingItems.Count)];
        
        ItemData resultCopy = ScriptableObject.CreateInstance<ItemData>();
        resultCopy.id = selectedItem.id;
        resultCopy.gun = selectedItem.gun;
        resultCopy.name = selectedItem.name;
        resultCopy.basePrice = selectedItem.basePrice;
        resultCopy.price = selectedItem.price;
        resultCopy.condition = selectedItem.condition;
        resultCopy.type = selectedItem.type;
        resultCopy.rarity = selectedItem.rarity;
        resultCopy.weight = selectedItem.weight;
        resultCopy.demandScore = 0;
        resultCopy.imageName = selectedItem.imageName;
        resultCopy.isLocked = false;

        return resultCopy;
    }

    /// <summary>
    /// Gets the next rarity that would result from the current selection.
    /// </summary>
    public string GetResultRarity()
    {
        if (_selectedRarity == null)
        {
            return null;
        }
        return GetNextRarity(_selectedRarity);
    }

    /// <summary>
    /// Gets the average condition of selected items.
    /// </summary>
    public string GetAverageCondition()
    {
        if (_selectedItems.Count == 0)
        {
            return null;
        }
        return CalculateResultCondition(_selectedItems);
    }
}
