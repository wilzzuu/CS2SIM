using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TradeUpManager : MonoBehaviour, IItemFilterScreen
{
    public static TradeUpManager Instance { get; private set; }

    [Header("Prefab & Containers")]
    public GameObject tradeUpItemPrefab;
    public Transform inventoryGrid;
    public Transform selectedItemsGrid;
    public Transform resultContainer;

    [Header("Controls")]
    public Button tradeUpButton;
    public Button rebuyButton;
    public Button clearButton;
    public Button previousPageButton;
    public Button nextPageButton;
    public TMP_InputField searchInput;
    public Button searchBtn;
    public TMP_InputField priceInput;
    public Button searchPriceBtn;
    public TMP_Dropdown sortDropdown;
    public Toggle ascendingToggle;

    [Header("Texts")]
    public TextMeshProUGUI selectedCountText;
    public TextMeshProUGUI resultInfoText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI pageIndicatorText;

    private const int RequiredItems = 10;
    private const int ItemsPerPage = 12;

    private readonly List<ItemData> _selectedItems = new List<ItemData>();
    private readonly List<ItemData> _lastTradeUpItems = new List<ItemData>();
    private readonly ItemFilter _filter = new ItemFilter();
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
        if (rebuyButton != null) rebuyButton.onClick.AddListener(RebuyLastTradeUp);
        if (clearButton != null) clearButton.onClick.AddListener(ClearSelection);
        if (previousPageButton != null) previousPageButton.onClick.AddListener(PreviousPage);
        if (nextPageButton != null) nextPageButton.onClick.AddListener(NextPage);
        if (searchBtn != null) searchBtn.onClick.AddListener(SearchItems);
        if (searchInput != null) searchInput.onValueChanged.AddListener(OnSearchInputChanged);
        if (searchPriceBtn != null) searchPriceBtn.onClick.AddListener(SearchPrices);
        if (priceInput != null) priceInput.onValueChanged.AddListener(OnPriceInputChanged);
        if (sortDropdown != null) sortDropdown.onValueChanged.AddListener(OnSortChanged);
        if (ascendingToggle != null) ascendingToggle.onValueChanged.AddListener(OnSortOrderChanged);

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnBalanceChanged += RefreshRebuyButton;
        }

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (rebuyButton != null) rebuyButton.onClick.RemoveListener(RebuyLastTradeUp);
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnBalanceChanged -= RefreshRebuyButton;
        }
        if (searchBtn != null) searchBtn.onClick.RemoveListener(SearchItems);
        if (searchInput != null) searchInput.onValueChanged.RemoveListener(OnSearchInputChanged);
        if (searchPriceBtn != null) searchPriceBtn.onClick.RemoveListener(SearchPrices);
        if (priceInput != null) priceInput.onValueChanged.RemoveListener(OnPriceInputChanged);
        if (sortDropdown != null) sortDropdown.onValueChanged.RemoveListener(OnSortChanged);
        if (ascendingToggle != null) ascendingToggle.onValueChanged.RemoveListener(OnSortOrderChanged);
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

        _lastTradeUpItems.Clear();
        _lastTradeUpItems.AddRange(_selectedItems);
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

    public void RebuyLastTradeUp()
    {
        if (_lastTradeUpItems.Count != RequiredItems)
        {
            SetStatus("Complete a trade up before rebuying its items.");
            return;
        }

        if (PlayerManager.Instance == null || InventoryManager.Instance == null)
        {
            Debug.LogError("Cannot rebuy trade up items: player or inventory manager is not set.", this);
            return;
        }

        if (!TryGetRebuyCost(out int totalCost))
        {
            SetStatus("Previous trade-up items are no longer available.");
            return;
        }

        if (PlayerManager.Instance.GetPlayerBalance() < totalCost)
        {
            SetStatus($"Not enough balance to rebuy these items. Cost: {Globals.FormatCurrencyStr(totalCost, Globals.EURO_DECIMAL)}€");
            return;
        }

        PlayerManager.Instance.DeductMoney(totalCost);

        List<ItemData> reboughtItems = new List<ItemData>(_lastTradeUpItems.Count);
        foreach (ItemData item in _lastTradeUpItems)
        {
            reboughtItems.Add(InventoryManager.Instance.AddItemToInventory(item));
        }

        _selectedItems.Clear();
        _selectedItems.AddRange(reboughtItems);
        _selectedRarity = _selectedItems[0].rarity;
        _selectedStatTrak = _selectedItems[0].isStatTrak;
        _currentPage = 0;

        LogUtil.Economy.LogEuro(
            "TRADE_UP_REBUY",
            -totalCost,
            PlayerManager.Instance.GetPlayerBalance(),
            $"{RequiredItems} items"
        );

        SelectionChanged();
        SetStatus("Previous trade-up items rebought!");
    }

    private bool TryGetRebuyCost(out int totalCost)
    {
        totalCost = 0;
        if (_lastTradeUpItems.Count != RequiredItems) return false;

        long cost = 0;

        foreach (ItemData item in _lastTradeUpItems)
        {
            if (item == null || string.IsNullOrEmpty(item.id)) return false;

            ItemData marketItem = Resources.Load<ItemData>($"ItemAssets/{item.id}");
            if (marketItem == null) return false;

            int itemCost = Globals.GetMarketBuyPrice(marketItem);
            if (itemCost < 0) return false;

            cost += itemCost;
            if (cost > int.MaxValue) return false;
        }

        totalCost = (int)cost;
        return true;
    }

    private void RefreshRebuyButton()
    {
        if (rebuyButton == null) return;

        rebuyButton.interactable =
            PlayerManager.Instance != null &&
            TryGetRebuyCost(out int totalCost) &&
            PlayerManager.Instance.GetPlayerBalance() >= totalCost;
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

        items = _filter.Apply(items);

        if (_selectedItems.Count > 0)
        {
            items = items.Where(i => i.rarity == _selectedRarity && i.isStatTrak == _selectedStatTrak);
        }

        string query = searchInput == null ? string.Empty : searchInput.text.Trim();
        if (!string.IsNullOrEmpty(query))
        {
            items = items.Where(item =>
                (item.name != null && item.name.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0) ||
                (item.id != null && item.id.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0));
        }

        if (priceInput != null && Globals.TryParseEuroInput(priceInput.text, out int priceLimit))
        {
            items = items.Where(item => item.price <= priceLimit);
        }

        if (sortDropdown != null && sortDropdown.value >= 0 && sortDropdown.value < sortDropdown.options.Count)
        {
            bool ascending = ascendingToggle == null || ascendingToggle.isOn;
            return Globals.SortItems(items, sortDropdown.options[sortDropdown.value].text, ascending);
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
        RefreshRebuyButton();
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

    public void OpenFilters()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("Cannot open trade up filters: InventoryManager instance is not set.", this);
            return;
        }

        IEnumerable<ItemData> filterItems = InventoryManager.Instance.GetUnlockedItems()
            .Where(item => GetNextRarity(item.rarity) != null);
        ItemFilterPopup.Open(inventoryGrid, _filter, filterItems, RefreshFilteredItems);
    }

    public void SortItems()
    {
        if (sortDropdown != null && sortDropdown.value >= 0 && sortDropdown.value < sortDropdown.options.Count &&
            sortDropdown.options[sortDropdown.value].text == "Random")
        {
            Globals.RandomizeItemSortOrder();
        }

        _currentPage = 0;
        RefreshUI();
    }

    public void SearchItems()
    {
        _currentPage = 0;
        RefreshUI();
    }

    public void SearchPrices()
    {
        _currentPage = 0;
        RefreshUI();
    }

    private void OnSearchInputChanged(string _)
    {
        if (searchInput == null || string.IsNullOrWhiteSpace(searchInput.text))
        {
            SearchItems();
        }
    }

    private void OnPriceInputChanged(string _)
    {
        if (priceInput == null || string.IsNullOrWhiteSpace(priceInput.text))
        {
            SearchPrices();
        }
    }

    private void OnSortChanged(int _) => SortItems();
    private void OnSortOrderChanged(bool _) => SortItems();
    private void RefreshFilteredItems() => SearchItems();

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
