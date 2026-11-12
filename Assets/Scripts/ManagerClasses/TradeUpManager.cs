using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TradeUpManager : MonoBehaviour
{
    public static TradeUpManager Instance { get; private set; }

    [Header("Prefab & Containers")]
    public GameObject tradeUpItemPrefab;
    public Transform inventoryGrid;
    public Transform selectedItemsGrid;
    public Transform resultContainer;

    [Header("Controls")]
    public Button tradeUpButton;
    public Button clearButton;
    public Button previousPageButton;
    public Button nextPageButton;

    [Header("Texts")]
    public TextMeshProUGUI selectedCountText;
    public TextMeshProUGUI resultInfoText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI pageIndicatorText;

    private const int RequiredItems = 10;
    private const int ItemsPerPage = 12;

    private readonly List<ItemData> _selectedItems = new List<ItemData>();
    private string _selectedRarity;
    private bool _selectedStatTrak;
    private int _currentPage;

    public delegate void SelectionChangedHandler();
    public event SelectionChangedHandler OnSelectionChanged;

    public delegate void TradeUpCompleteHandler(ItemData resultItem);
    public event TradeUpCompleteHandler OnTradeUpComplete;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (tradeUpButton != null) tradeUpButton.onClick.AddListener(() => ExecuteTradeUp());
        if (clearButton != null) clearButton.onClick.AddListener(ClearSelection);
        if (previousPageButton != null) previousPageButton.onClick.AddListener(PreviousPage);
        if (nextPageButton != null) nextPageButton.onClick.AddListener(NextPage);

        RefreshUI();
    }

    public bool AddItemToSelection(ItemData item)
    {
        if (item == null || item.isLocked) return false;
        if (_selectedItems.Contains(item)) return false;
        if (_selectedItems.Count >= RequiredItems) return false;
        if (GetNextRarity(item.rarity) == null) return false;

        if (_selectedItems.Count > 0)
        {
            if (item.rarity != _selectedRarity || item.isStatTrak != _selectedStatTrak)
            {
                SetStatus("All selected items must have matching rarity and StatTrak status.");
                return false;
            }
        }
        else
        {
            _selectedRarity = item.rarity;
            _selectedStatTrak = item.isStatTrak;
        }

        _selectedItems.Add(item);
        SelectionChanged();
        return true;
    }

    public bool RemoveItemFromSelection(ItemData item)
    {
        if (!_selectedItems.Remove(item)) return false;

        if (_selectedItems.Count == 0) _selectedRarity = null;
        SelectionChanged();
        return true;
    }

    public void ClearSelection()
    {
        _selectedItems.Clear();
        _selectedRarity = null;
        SelectionChanged();
    }

    public List<ItemData> GetSelectedItems() => new List<ItemData>(_selectedItems);

    public bool IsSelectionValid() => _selectedItems.Count == RequiredItems;

    public string GetResultRarity() => _selectedRarity == null ? null : GetNextRarity(_selectedRarity);

    public string GetAverageCondition() => _selectedItems.Count == 0 ? null : CalculateResultCondition(_selectedItems);

    // Consumes the 10 selected items, grants the traded item and returns it (null on failure).
    public ItemData ExecuteTradeUp()
    {
        if (!IsSelectionValid())
        {
            SetStatus($"Select {RequiredItems} items.");
            return null;
        }

        if (_selectedItems.Any(i => i.isLocked || !InventoryManager.Instance.HasItem(i)))
        {
            SetStatus("Some selected items are no longer available.");
            ClearSelection();
            return null;
        }

        string nextRarity = GetNextRarity(_selectedRarity);
        if (nextRarity == null)
        {
            SetStatus("No higher rarity available.");
            return null;
        }

        ItemData result = SelectRandomItem(nextRarity, CalculateResultCondition(_selectedItems), _selectedStatTrak);
        if (result == null)
        {
            SetStatus("No matching item found for this trade up.");
            return null;
        }

        InventoryManager.Instance.RemoveItemsFromInventory(_selectedItems);
        result = InventoryManager.Instance.AddItemToInventory(result);
        CollectionManager.RecordItem(result);

        _selectedItems.Clear();
        _selectedRarity = null;
        _currentPage = 0;

        SelectionChanged(false);
        ShowResult(result);
        SetStatus("Trade Up successful!");
        OnTradeUpComplete?.Invoke(result);
        return result;
    }

    private void SelectionChanged(bool clearResult = true)
    {
        if (clearResult) ClearContainer(resultContainer);
        RefreshUI();
        OnSelectionChanged?.Invoke();
    }

    private static string GetNextRarity(string rarity)
    {
        if (rarity == null || !RarityData.Order.TryGetValue(rarity, out int order)) return null;

        foreach (var pair in RarityData.Order)
        {
            if (pair.Value == order + 1) return pair.Key;
        }
        return null;
    }

    private static string CalculateResultCondition(List<ItemData> items)
    {
        float average = (float)items.Average(i => ConditionData.Order.TryGetValue(i.condition, out int o) ? o : 3);
        int rounded = Mathf.Clamp(Mathf.FloorToInt(average + 0.5f), 1, ConditionData.Order.Count);
        return ConditionData.Order.First(p => p.Value == rounded).Key;
    }

    private static int ConditionDistance(ItemData item, int target)
    {
        return Mathf.Abs((ConditionData.Order.TryGetValue(item.condition, out int o) ? o : 0) - target);
    }

    private static ItemData SelectRandomItem(string rarity, string condition, bool statTrak)
    {
        List<ItemData> pool = Resources.LoadAll<ItemData>("ItemAssets")
            .Where(i => i.rarity == rarity && i.isStatTrak == statTrak)
            .ToList();
        if (pool.Count == 0) return null;

        // Some items don't exist in every condition; fall back to the closest available one.
        int target = ConditionData.Order[condition];
        int best = pool.Min(i => ConditionDistance(i, target));
        List<ItemData> candidates = pool.Where(i => ConditionDistance(i, target) == best).ToList();
        return candidates[Random.Range(0, candidates.Count)];
    }

    private List<ItemData> GetAvailableInventoryItems()
    {
        IEnumerable<ItemData> items = InventoryManager.Instance.GetUnlockedItems()
            .Where(i => !_selectedItems.Contains(i) && GetNextRarity(i.rarity) != null);

        if (_selectedItems.Count > 0)
        {
            items = items.Where(i => i.rarity == _selectedRarity && i.isStatTrak == _selectedStatTrak);
        }

        return Globals.SortItems(items, "Rarity", true);
    }

    private void RefreshUI()
    {
        if (InventoryManager.Instance == null) return;

        List<ItemData> available = GetAvailableInventoryItems();
        int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)available.Count / ItemsPerPage));
        _currentPage = Mathf.Clamp(_currentPage, 0, totalPages - 1);

        ClearContainer(inventoryGrid);
        foreach (ItemData item in available.Skip(_currentPage * ItemsPerPage).Take(ItemsPerPage))
        {
            SpawnItem(inventoryGrid, item, i => AddItemToSelection(i));
        }

        ClearContainer(selectedItemsGrid);
        foreach (ItemData item in _selectedItems)
        {
            SpawnItem(selectedItemsGrid, item, i => RemoveItemFromSelection(i));
        }

        if (pageIndicatorText != null) pageIndicatorText.text = $"Page {_currentPage + 1} of {totalPages}";
        if (previousPageButton != null) previousPageButton.interactable = _currentPage > 0;
        if (nextPageButton != null) nextPageButton.interactable = _currentPage < totalPages - 1;
        if (selectedCountText != null) selectedCountText.text = $"{_selectedItems.Count}/{RequiredItems}";
        if (tradeUpButton != null) tradeUpButton.interactable = IsSelectionValid();
        if (clearButton != null) clearButton.interactable = _selectedItems.Count > 0;

        if (resultInfoText != null)
        {
            resultInfoText.text = _selectedItems.Count == 0
                ? "-"
                : $"{(_selectedStatTrak ? Globals.StatTrakUIText + "\n" : "")}{GetResultRarity()}\n{GetAverageCondition()}";
        }
    }

    private void ShowResult(ItemData item)
    {
        ClearContainer(resultContainer);
        SpawnItem(resultContainer, item, null);
    }

    private void SpawnItem(Transform parent, ItemData item, System.Action<ItemData> onClick)
    {
        if (parent == null || tradeUpItemPrefab == null) return;

        GameObject obj = Instantiate(tradeUpItemPrefab, parent);
        obj.GetComponent<TradeUpItem>().Setup(item, onClick);
    }

    private static void ClearContainer(Transform container)
    {
        if (container == null) return;

        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    public void NextPage()
    {
        _currentPage++;
        RefreshUI();
    }

    public void PreviousPage()
    {
        _currentPage--;
        RefreshUI();
    }
}
