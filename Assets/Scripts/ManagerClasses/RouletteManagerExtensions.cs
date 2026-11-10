using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Extension methods for RouletteManager that handle item purchasing and replay functionality.
/// </summary>
public static class RouletteManagerExtensions
{
    private static RoulettePurchaseManager _purchaseManager = new RoulettePurchaseManager();
    private static List<ItemData> _lastRoundWonItems = new List<ItemData>();

    /// <summary>
    /// Records the outcome of a roulette round and prepares items for purchase.
    /// Call this after a roulette spin completes.
    /// </summary>
    public static void RecordRoundOutcome(this RouletteManager manager, List<ItemData> playedItems, List<ItemData> wonItems)
    {
        if (RoulettePurchaseSystem.Instance == null)
        {
            Debug.LogWarning("RoulettePurchaseSystem.Instance is not initialized. Skipping round outcome recording.");
            return;
        }

        RoulettePurchaseSystem.Instance.RecordPlayedItems(playedItems);
        RoulettePurchaseSystem.Instance.RecordWonItems(wonItems);
        _lastRoundWonItems.Clear();
        _lastRoundWonItems.AddRange(wonItems);
        _purchaseManager.Initialize(wonItems);
    }

    /// <summary>
    /// Gets the current purchase manager for won items.
    /// </summary>
    public static RoulettePurchaseManager GetPurchaseManager(this RouletteManager manager)
    {
        return _purchaseManager;
    }

    /// <summary>
    /// Attempts to purchase an item and add it to inventory for replay.
    /// Requires sufficient balance.
    /// </summary>
    public static bool TryPurchaseItemForReplay(this RouletteManager manager, ItemData item)
    {
        if (RoulettePurchaseSystem.Instance == null)
        {
            Debug.LogWarning("RoulettePurchaseSystem.Instance is not initialized. Cannot purchase item.");
            return false;
        }
        return RoulettePurchaseSystem.Instance.TryPurchaseItem(item);
    }

    /// <summary>
    /// Attempts to purchase multiple items for replay as a batch.
    /// All items must be purchasable or the purchase is cancelled.
    /// </summary>
    public static bool TryPurchaseItemsForReplay(this RouletteManager manager, List<ItemData> items)
    {
        if (RoulettePurchaseSystem.Instance == null)
        {
            Debug.LogWarning("RoulettePurchaseSystem.Instance is not initialized. Cannot purchase items.");
            return false;
        }
        return RoulettePurchaseSystem.Instance.TryPurchaseItems(items);
    }

    /// <summary>
    /// Gets the items won in the last round.
    /// </summary>
    public static List<ItemData> GetLastRoundWonItems(this RouletteManager manager)
    {
        return new List<ItemData>(_lastRoundWonItems);
    }

    /// <summary>
    /// Checks if player can afford to purchase an item.
    /// </summary>
    public static bool CanAffordItem(this RouletteManager manager, ItemData item)
    {
        if (RoulettePurchaseSystem.Instance == null)
        {
            Debug.LogWarning("RoulettePurchaseSystem.Instance is not initialized.");
            return false;
        }
        return RoulettePurchaseSystem.Instance.CanAfford(item);
    }

    /// <summary>
    /// Gets the total cost to purchase selected items.
    /// </summary>
    public static int GetPurchaseCost(this RouletteManager manager, List<ItemData> items)
    {
        if (RoulettePurchaseSystem.Instance == null)
        {
            Debug.LogWarning("RoulettePurchaseSystem.Instance is not initialized.");
            return 0;
        }
        return RoulettePurchaseSystem.Instance.CalculateTotalCost(items);
    }

    /// <summary>
    /// Clears purchase history.
    /// </summary>
    public static void ClearPurchaseHistory(this RouletteManager manager)
    {
        if (RoulettePurchaseSystem.Instance != null)
        {
            RoulettePurchaseSystem.Instance.Clear();
        }
        _purchaseManager.Clear();
        _lastRoundWonItems.Clear();
    }
}
