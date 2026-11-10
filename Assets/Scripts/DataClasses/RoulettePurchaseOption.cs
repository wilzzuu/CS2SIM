using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents items available for purchase after a roulette round.
/// Tracks which items were won and their purchase status.
/// </summary>
[System.Serializable]
public class RoulettePurchaseOption
{
    public ItemData Item;
    public int Price;
    public bool IsPurchased;
    public bool IsAffordable;

    public RoulettePurchaseOption(ItemData item, bool isAffordable)
    {
        Item = item;
        Price = item.price;
        IsPurchased = false;
        IsAffordable = isAffordable;
    }

    public void UpdateAffordability(bool affordable)
    {
        IsAffordable = affordable;
    }
}

/// <summary>
/// Manages the set of items available for purchase after a roulette session.
/// </summary>
public class RoulettePurchaseManager
{
    private List<RoulettePurchaseOption> _availablePurchases = new List<RoulettePurchaseOption>();
    private int _totalCost;
    private int _selectedCount;

    public List<RoulettePurchaseOption> AvailablePurchases => new List<RoulettePurchaseOption>(_availablePurchases);
    public int TotalCost => _totalCost;
    public int SelectedCount => _selectedCount;

    /// <summary>
    /// Initializes the purchase manager with a list of won items.
    /// </summary>
    public void Initialize(List<ItemData> wonItems)
    {
        _availablePurchases.Clear();
        _totalCost = 0;
        _selectedCount = 0;

        if (wonItems == null) return;

        foreach (var item in wonItems)
        {
            if (item != null)
            {
                bool isAffordable = RoulettePurchaseSystem.Instance.CanAfford(item);
                _availablePurchases.Add(new RoulettePurchaseOption(item, isAffordable));
            }
        }
    }

    /// <summary>
    /// Toggles purchase selection for a specific item.
    /// </summary>
    public bool ToggleItemSelection(int index)
    {
        if (index < 0 || index >= _availablePurchases.Count)
        {
            Debug.LogWarning("Invalid purchase option index.");
            return false;
        }

        var option = _availablePurchases[index];
        if (!option.IsAffordable)
        {
            Debug.LogWarning("Cannot select an unaffordable item.");
            return false;
        }

        option.IsPurchased = !option.IsPurchased;
        RecalculateTotalCost();
        return true;
    }

    /// <summary>
    /// Selects an item for purchase.
    /// </summary>
    public bool SelectItem(int index)
    {
        if (index < 0 || index >= _availablePurchases.Count)
        {
            Debug.LogWarning("Invalid purchase option index.");
            return false;
        }

        var option = _availablePurchases[index];
        if (!option.IsAffordable)
        {
            Debug.LogWarning("Cannot select an unaffordable item.");
            return false;
        }

        if (!option.IsPurchased)
        {
            option.IsPurchased = true;
            RecalculateTotalCost();
        }
        return true;
    }

    /// <summary>
    /// Deselects an item from purchase.
    /// </summary>
    public bool DeselectItem(int index)
    {
        if (index < 0 || index >= _availablePurchases.Count)
        {
            Debug.LogWarning("Invalid purchase option index.");
            return false;
        }

        var option = _availablePurchases[index];
        if (option.IsPurchased)
        {
            option.IsPurchased = false;
            RecalculateTotalCost();
        }
        return true;
    }

    /// <summary>
    /// Gets the selected items for purchase.
    /// </summary>
    public List<ItemData> GetSelectedItems()
    {
        var selected = new List<ItemData>();
        foreach (var option in _availablePurchases)
        {
            if (option.IsPurchased)
            {
                selected.Add(option.Item);
            }
        }
        return selected;
    }

    /// <summary>
    /// Gets the cost of selected items.
    /// </summary>
    public int GetSelectedItemsCost()
    {
        int cost = 0;
        foreach (var option in _availablePurchases)
        {
            if (option.IsPurchased)
            {
                cost += option.Price;
            }
        }
        return cost;
    }

    /// <summary>
    /// Checks if the player can afford all selected items.
    /// </summary>
    public bool CanAffordSelected()
    {
        return PlayerManager.Instance.player.balance >= GetSelectedItemsCost();
    }

    /// <summary>
    /// Purchases all selected items.
    /// </summary>
    public bool PurchaseSelected()
    {
        List<ItemData> selectedItems = GetSelectedItems();
        if (selectedItems.Count == 0)
        {
            Debug.LogWarning("No items selected for purchase.");
            return false;
        }

        return RoulettePurchaseSystem.Instance.TryPurchaseItems(selectedItems);
    }

    /// <summary>
    /// Selects all affordable items.
    /// </summary>
    public void SelectAllAffordable()
    {
        foreach (var option in _availablePurchases)
        {
            if (option.IsAffordable && !option.IsPurchased)
            {
                option.IsPurchased = true;
            }
        }
        RecalculateTotalCost();
    }

    /// <summary>
    /// Clears all selections.
    /// </summary>
    public void ClearSelection()
    {
        foreach (var option in _availablePurchases)
        {
            option.IsPurchased = false;
        }
        RecalculateTotalCost();
    }

    /// <summary>
    /// Recalculates total cost and selected count.
    /// </summary>
    private void RecalculateTotalCost()
    {
        _totalCost = 0;
        _selectedCount = 0;

        foreach (var option in _availablePurchases)
        {
            if (option.IsPurchased)
            {
                _totalCost += option.Price;
                _selectedCount++;
            }
        }
    }

    /// <summary>
    /// Updates affordability status for all items based on current balance.
    /// </summary>
    public void UpdateAffordability()
    {
        foreach (var option in _availablePurchases)
        {
            bool isAffordable = RoulettePurchaseSystem.Instance.CanAfford(option.Item);
            option.UpdateAffordability(isAffordable);
        }
    }

    /// <summary>
    /// Clears all purchase options.
    /// </summary>
    public void Clear()
    {
        _availablePurchases.Clear();
        _totalCost = 0;
        _selectedCount = 0;
    }

    /// <summary>
    /// Gets total number of available purchase options.
    /// </summary>
    public int GetTotalOptionsCount()
    {
        return _availablePurchases.Count;
    }

    /// <summary>
    /// Gets number of affordable items.
    /// </summary>
    public int GetAffordableCount()
    {
        int count = 0;
        foreach (var option in _availablePurchases)
        {
            if (option.IsAffordable) count++;
        }
        return count;
    }
}
