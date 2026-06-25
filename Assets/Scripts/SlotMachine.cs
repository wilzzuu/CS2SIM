using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SlotMachine : MonoBehaviour
{
    [Header("Machine")]
    [SerializeField] private float spinCost = 5f;

    [Header("Pool")]
    [SerializeField] private List<ItemData> slotPool = new();

    [Header("Win Chances")]
    [Range(0f, 1f)]
    [SerializeField] private float winChance = 0.10f;

    [Range(0f, 1f)]
    [SerializeField] private float nearMissChance = 0.20f;

    public SlotResult LastResult { get; private set; }

    public bool Spin()
    {
        if (PlayerManager.Instance.GetPlayerBalance() < spinCost)
        {
            Debug.Log("Not enough balance.");
            return false;
        }

        PlayerManager.Instance.DeductMoney(spinCost);

        LastResult = GenerateResult();

        Debug.Log(
            $"{LastResult.reel1.name} | " +
            $"{LastResult.reel2.name} | " +
            $"{LastResult.reel3.name}");

        if (LastResult.isWin)
        {
            InventoryManager.Instance.AddItemToInventory(
                LastResult.winningItem);
        }

        return true;
    }

    private SlotResult GenerateResult()
    {
        SlotResult result = new();

        float roll = Random.value;

        // WIN
        if (roll < winChance)
        {
            ItemData item = GetWeightedItem();

            result.reel1 = item;
            result.reel2 = item;
            result.reel3 = item;

            result.isWin = true;
            result.winningItem = item;

            return result;
        }

        // NEAR MISS
        if (roll < winChance + nearMissChance)
        {
            ItemData item = GetWeightedItem();

            result.reel1 = item;
            result.reel2 = item;
            result.reel3 = GetWeightedItem();

            while (result.reel3 == item)
                result.reel3 = GetWeightedItem();

            return result;
        }

        // LOSS
        result.reel1 = GetWeightedItem();
        result.reel2 = GetWeightedItem();
        result.reel3 = GetWeightedItem();

        return result;
    }

    private ItemData GetWeightedItem()
    {
        if (slotPool.Count == 0)
            return null;

        float totalWeight = 0f;

        foreach (var item in slotPool)
        {
            totalWeight += GetItemWeight(item);
        }

        float randomWeight =
            Random.Range(0f, totalWeight);

        float currentWeight = 0f;

        foreach (var item in slotPool)
        {
            currentWeight += GetItemWeight(item);

            if (randomWeight <= currentWeight)
                return item;
        }

        return slotPool.Last();
    }

    private float GetItemWeight(ItemData item)
    {
        float value = Mathf.Max(item.price, 0.01f);

        // Cheap items appear more often
        return 1f / value;
    }

    public ItemData RandomPoolItem()
    {
        return slotPool[Random.Range(0, slotPool.Count)];
    }

    [ContextMenu("Auto Fill Pool")]
    public void AutoFillPool()
    {
        slotPool = Resources
            .LoadAll<ItemData>("ItemAssets")
            .Where(x =>
                x != null &&
                !x.isStatTrak &&
                x.price >= 1f &&
                x.price <= 100f)
            .ToList();

        Debug.Log(
            $"Loaded {slotPool.Count} items.");
    }
}

[System.Serializable]
public class SlotResult
{
    public ItemData reel1;
    public ItemData reel2;
    public ItemData reel3;

    public bool isWin;

    public ItemData winningItem;
}