using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the purchase and replay logic for roulette items.
/// Players can purchase items they just played/won and immediately use them in a replay.
/// </summary>
public class RoulettePurchaseSystem : MonoBehaviour
{
    public static RoulettePurchaseSystem Instance { get; private set; }

    private List<ItemData> _lastPlayedItems = new List<ItemData>();
    private List<ItemData> _lastWonItems = new List<ItemData>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Records the items that were played in the last roulette round.
    /// </summary>
    public void RecordPlayedItems(List<ItemData> items)
    {
        _lastPlayedItems.Clear();
        _lastPlayedItems.AddRange(items);
    }

    /// <summary>
    /// Records the items that were won in the last roulette round.
    /// </summary>
    public void RecordWonItems(List<ItemData> items)
    {
        _lastWonItems.Clear();
        _lastWonItems.AddRange(items);
    }

    /// <summary>
    /// Gets the items from the last played round.
    /// </summary>
    public List<ItemData> GetLastPlayedItems()
    {
        return new List<ItemData>(_lastPlayedItems);
    }

    /// <summary>
    /// Gets the items from the last won round.
    /// </summary>
    public List<ItemData> GetLastWonItems()
    {
        return new List<ItemData>(_lastWonItems);
    }

    /// <summary>
    /// Attempts to purchase an item for use in replay.
    /// Checks if player has sufficient balance and adds item to inventory.
    /// </summary>
    /// <param name="item">The item to purchase</param>
    /// <returns>True if purchase was successful, false if insufficient balance</returns>
    public bool TryPurchaseItem(ItemData item)
    {
        if (item == null)
        {
            Debug.LogWarning("Cannot purchase a null item.");
            return false;
        }

        int itemPrice = item.price;
        if (PlayerManager.Instance.player.balance < itemPrice)
        {
            Debug.LogWarning($"Insufficient balance to purchase item. Required: {itemPrice}€, Available: {PlayerManager.Instance.player.balance}€");
            return false;
        }

        PlayerManager.Instance.DeductMoney(itemPrice);
        InventoryManager.Instance.AddItemToInventory(item);

        LogUtil.Economy.LogEuro(
            "ROULETTE_PURCHASE",
            -itemPrice,
            PlayerManager.Instance.player.balance
        );

        return true;
    }

    /// <summary>
    /// Attempts to purchase multiple items for replay.
    /// All items must be purchasable with current balance, or none will be purchased.
    /// </summary>
    /// <param name="items">The items to purchase</param>
    /// <returns>True if all purchases were successful, false if any failed</returns>
    public bool TryPurchaseItems(List<ItemData> items)
    {
        if (items == null || items.Count == 0)
        {
            Debug.LogWarning("Cannot purchase an empty item list.");
            return false;
        }

        int totalCost = 0;
        foreach (var item in items)
        {
            if (item == null)
            {
                Debug.LogWarning("Cannot purchase a null item in batch.");
                return false;
            }
            totalCost += item.price;
        }

        if (PlayerManager.Instance.player.balance < totalCost)
        {
            Debug.LogWarning($"Insufficient balance to purchase items. Required: {totalCost}€, Available: {PlayerManager.Instance.player.balance}€");
            return false;
        }

        PlayerManager.Instance.DeductMoney(totalCost);

        foreach (var item in items)
        {
            InventoryManager.Instance.AddItemToInventory(item);
        }

        LogUtil.Economy.LogEuro(
            "ROULETTE_PURCHASE_BATCH",
            -totalCost,
            PlayerManager.Instance.player.balance
        );

        return true;
    }

    /// <summary>
    /// Calculates the total cost of purchasing items.
    /// </summary>
    public int CalculateTotalCost(List<ItemData> items)
    {
        int total = 0;
        if (items != null)
        {
            foreach (var item in items)
            {
                if (item != null)
                {
                    total += item.price;
                }
            }
        }
        return total;
    }

    /// <summary>
    /// Checks if player has sufficient balance to purchase items.
    /// </summary>
    public bool CanAfford(List<ItemData> items)
    {
        if (items == null || items.Count == 0) return true;
        return PlayerManager.Instance.player.balance >= CalculateTotalCost(items);
    }

    /// <summary>
    /// Checks if player has sufficient balance to purchase a single item.
    /// </summary>
    public bool CanAfford(ItemData item)
    {
        if (item == null) return true;
        return PlayerManager.Instance.player.balance >= item.price;
    }

    /// <summary>
    /// Clears the purchase history.
    /// </summary>
    public void Clear()
    {
        _lastPlayedItems.Clear();
        _lastWonItems.Clear();
    }
}
