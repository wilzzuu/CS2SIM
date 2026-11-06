using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections;
using System;

public class MarketManager : MonoBehaviour
{
    public static MarketManager Instance { get; private set; }
    public Transform buyCatalogContent;
    public Transform sellCatalogContent;
    public GameObject marketplaceItemPrefab;

    public Button showBuyTabButton;
    public Button showSellTabButton;
    public GameObject buyScrollView;
    public GameObject sellScrollView;

    public TMP_InputField searchInput;
    public Button searchBtn;
    public TMP_InputField priceInput;
    public Button searchPriceBtn;
    public TMP_Dropdown sortDropdown;
    public Toggle toggleSortingOrder;
    public Toggle filterByBalanceToggle;
    public Button sellAllButton;

    public TextMeshProUGUI currentPageText;
    public TextMeshProUGUI maxPageText;

    private List<ItemData> _allItems = new List<ItemData>();
    private List<ItemData> _inventoryItems = new List<ItemData>();
    private readonly Dictionary<ItemData, MarketplaceItem> _marketplaceItems = new Dictionary<ItemData, MarketplaceItem>();

    private int currentPage;

    const int ItemsPerPage = 30;

    private bool _isBuyingTabActive;

    private const float FluctuationIntensity = 0.1f;
    private const int ItemsToFluctuate = 20;
    private const float MarketEventInterval = 120f;
    private DateTime _lastUpdateTimestamp;
    public float interactionCooldown = 0.2f;
    private float _lastInteractionTime;
    private const string LastUpdateKey = "LastMarketUpdate";

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

    void Start()
    {
        LoadAllGameItems();
        LoadInventoryItems();

        ShowSellTab();

        showBuyTabButton.onClick.AddListener(ShowBuyTab);
        showSellTabButton.onClick.AddListener(ShowSellTab);

        searchBtn.onClick.AddListener(SearchItems);
        searchInput.onValueChanged.AddListener(delegate { ClearSearch(); });
        searchPriceBtn.onClick.AddListener(SearchPrices);
        priceInput.onValueChanged.AddListener(delegate { ClearPriceSearch(); });

        sortDropdown.onValueChanged.AddListener(delegate { SortItems(); });
        toggleSortingOrder.onValueChanged.AddListener(delegate { SortItems(); });
        filterByBalanceToggle.onValueChanged.AddListener(delegate { DisplayPage(0); });
        PlayerManager.Instance.OnBalanceChanged += RefreshBalanceFilter;

        LoadLastUpdateTimestamp();
        ApplyRealTimeFluctuations();
        StartCoroutine(MarketFluctuationCoroutine());
        StartCoroutine(DemandScoreDecayCoroutine());
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private void UpdateCurrentTab()
    {
        DisplayPage(currentPage);
    }

    void LoadLastUpdateTimestamp()
    {
        string lastUpdateString = PlayerPrefs.GetString(LastUpdateKey, DateTime.UtcNow.ToString());
        _lastUpdateTimestamp = DateTime.Parse(lastUpdateString);
    }

    void ApplyRealTimeFluctuations()
    {
        DateTime currentTime = DateTime.UtcNow;
        TimeSpan elapsedTime = currentTime - _lastUpdateTimestamp;

        int intervals = (int)(elapsedTime.TotalSeconds / MarketEventInterval);

        if (intervals > 0)
        {
            for (int i = 0; i < intervals; i++)
            {
                List<ItemData> itemsForFluctuation = _allItems.OrderBy(_ => UnityEngine.Random.value).Take(ItemsToFluctuate).ToList();

                foreach (var item in itemsForFluctuation)
                {
                    ApplyFluctuation(item);
                }
            }

            _lastUpdateTimestamp = currentTime;
            PlayerPrefs.SetString(LastUpdateKey, _lastUpdateTimestamp.ToString());
            PlayerPrefs.Save();
        }
    }

    void LoadAllGameItems()
    {
        _allItems = Resources.LoadAll<ItemData>("ItemAssets").ToList();
    }

    void LoadInventoryItems()
    {
        _inventoryItems = InventoryManager.Instance.GetAllItems();
    }

    public void ShowBuyTab()
    {
        _isBuyingTabActive = true;
        buyScrollView.SetActive(true);
        sellScrollView.SetActive(false);
        sellAllButton.gameObject.SetActive(false);

        currentPage = 0;
        DisplayPage(currentPage);
    }

    public void ShowSellTab()
    {
        _isBuyingTabActive = false;
        buyScrollView.SetActive(false);
        sellScrollView.SetActive(true);
        sellAllButton.gameObject.SetActive(true);
        
        LoadInventoryItems();
        
        currentPage = 0;
        DisplayPage(currentPage);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    void RefreshBalanceFilter()
    {
        if (_isBuyingTabActive && filterByBalanceToggle.isOn)
        {
            DisplayPage(0);
        }
    }

    void DisplayPage(int page)
    {
        Transform currentCatalogGrid = _isBuyingTabActive ? buyCatalogContent : sellCatalogContent;
        List<ItemData> displayedItems = _isBuyingTabActive ? _allItems : _inventoryItems;

        int pageCount = Mathf.CeilToInt((float)displayedItems.Count / ItemsPerPage);

        if (currentCatalogGrid == null) return;

        if (_isBuyingTabActive && filterByBalanceToggle.isOn)
        {
            int playerBalance = PlayerManager.Instance.GetPlayerBalance();
            displayedItems = displayedItems.Where(item => item.price * Globals.MarketBuyPriceModifier <= playerBalance).ToList();
            pageCount = Mathf.CeilToInt((float)displayedItems.Count / ItemsPerPage);
        }

        foreach (Transform child in currentCatalogGrid)
        {
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }

        int startIndex = page * ItemsPerPage;
        int endIndex = Mathf.Min(startIndex + ItemsPerPage, displayedItems.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            GameObject itemObj = Instantiate(marketplaceItemPrefab, currentCatalogGrid);
            MarketplaceItem marketplaceItem = itemObj.GetComponent<MarketplaceItem>();
            marketplaceItem.Setup(displayedItems[i], _isBuyingTabActive);
            _marketplaceItems[displayedItems[i]] = marketplaceItem;
        }

        currentPageText.text = ((pageCount > 0) ? page+1 : page).ToString();
        maxPageText.text = pageCount.ToString();
    }

    public void NextPage()
    {
        int itemsCount = _isBuyingTabActive ? _allItems.Count : _inventoryItems.Count;
        int pageCount = Mathf.CeilToInt((float)itemsCount / ItemsPerPage);

        if (currentPage < pageCount - 1)
        {
            currentPage++;
            DisplayPage(currentPage);
        }
    }

    public void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            DisplayPage(currentPage);
        }
    }

    public void ClearSearch()
    {
        if (string.IsNullOrWhiteSpace(searchInput.text))
        {
            LoadAllGameItems();
            LoadInventoryItems();
        }
    }

    public void ClearPriceSearch()
    {
        if (string.IsNullOrWhiteSpace(priceInput.text))
        {
            LoadAllGameItems();
            LoadInventoryItems();
        }
    }

    public void SortItems()
    {
        if (_isBuyingTabActive)
        {
            _allItems = new List<ItemData>(_allItems);
            string sortCriteria = sortDropdown.options[sortDropdown.value].text;
            bool ascending = toggleSortingOrder.isOn;

            _allItems = SortAllItemsByCriteria(sortCriteria, ascending);
        }
        else
        {
            _inventoryItems = new List<ItemData>(_inventoryItems);
            string sortCriteria = sortDropdown.options[sortDropdown.value].text;
            bool ascending = toggleSortingOrder.isOn;
            
            _inventoryItems = SortInventoryItemsByCriteria(sortCriteria, ascending);
        }
        UpdateCurrentTab();
    }
    public void SearchItems()
    {
        string query = searchInput.text.ToLower();

        if (string.IsNullOrWhiteSpace(query))
        {
            if (_isBuyingTabActive)
            {
                LoadAllGameItems();
                SortItems();
            }
            else
            {
                LoadAllGameItems();
                SortItems();
            }
        }
        else
        {
            if (_isBuyingTabActive)
            {
                _allItems = _allItems.Where(item =>
                    item.name.ToLower().Contains(query) ||
                    item.id.ToString().Contains(query)).ToList();
            }
            else
            {
                _inventoryItems = _inventoryItems.Where(item =>
                    item.name.ToLower().Contains(query) ||
                    item.id.ToString().Contains(query)).ToList();
            }
        }
        UpdateCurrentTab();
    }

    public void SearchPrices()
    {
        string query = priceInput.text;
        if (string.IsNullOrWhiteSpace(query))
        {
            if (_isBuyingTabActive)
            {
                LoadAllGameItems();
                SortItems();
            }
            else
            {
                LoadInventoryItems();
                SortItems();
            }
        }

        if (Globals.TryParseEuroInput(query, out int itemPriceQuery))
        {
            if (_isBuyingTabActive)
            {
                _allItems = _allItems.Where(item => item.price * Globals.MarketBuyPriceModifier <= itemPriceQuery).ToList();
            }
            else
            {
                _inventoryItems = _inventoryItems.Where(item => item.price <= itemPriceQuery).ToList();
            }
        }
        UpdateCurrentTab();
    }

    private List<ItemData> SortAllItemsByCriteria(string criteria, bool ascending)
    {
        return criteria switch
        {
            "Type" => ascending
                ? _allItems.OrderBy(i => i.type).ToList()
                : _allItems.OrderByDescending(i => i.type).ToList(),
            "Rarity" => ascending
                ? _allItems
                    .OrderBy(i => RarityData.Order.ContainsKey(i.rarity) ? RarityData.Order[i.rarity] : int.MaxValue)
                    .ToList()
                : _allItems
                    .OrderByDescending(i => RarityData.Order.ContainsKey(i.rarity) ? RarityData.Order[i.rarity] : int.MinValue)
                    .ToList(),
            "Gun" => ascending
                ? _allItems.OrderBy(i => i.gun).ToList()
                : _allItems.OrderByDescending(i => i.gun).ToList(),
            "Condition" => ascending
                ? _allItems
                    .OrderBy(i => ConditionData.Order.ContainsKey(i.condition) ? RarityData.Order[i.condition] : int.MaxValue)
                    .ToList()
                : _allItems
                    .OrderByDescending(i => ConditionData.Order.ContainsKey(i.condition) ? RarityData.Order[i.condition] : int.MinValue)
                    .ToList(),
            "Price" => ascending
                ? _allItems.OrderBy(i => i.price).ToList()
                : _allItems.OrderByDescending(i => i.price).ToList(),
            "Random" => ShuffleItems(_allItems),
            _ => _allItems
        };
    }
    
    private List<ItemData> SortInventoryItemsByCriteria(string criteria, bool ascending)
    {
        return criteria switch
        {
            "Type" => ascending
                ? _inventoryItems.OrderBy(i => i.type).ToList()
                : _inventoryItems.OrderByDescending(i => i.type).ToList(),
            "Rarity" => ascending
                ? _inventoryItems
                    .OrderBy(i => RarityData.Order.ContainsKey(i.rarity) ? RarityData.Order[i.rarity] : int.MaxValue)
                    .ToList()
                : _inventoryItems
                    .OrderByDescending(i => RarityData.Order.ContainsKey(i.rarity) ? RarityData.Order[i.rarity] : int.MinValue)
                    .ToList(),
            "Gun" => ascending
                ? _inventoryItems.OrderBy(i => i.gun).ToList()
                : _inventoryItems.OrderByDescending(i => i.gun).ToList(),
            "Condition" => ascending
                ? _inventoryItems
                    .OrderBy(i => ConditionData.Order.ContainsKey(i.condition) ? ConditionData.Order[i.condition] : int.MaxValue)
                    .ToList()
                : _inventoryItems
                    .OrderByDescending(i => ConditionData.Order.ContainsKey(i.condition) ? ConditionData.Order[i.condition] : int.MinValue)
                    .ToList(),
            "Price" => ascending
                ? _inventoryItems.OrderBy(i => i.price).ToList()
                : _inventoryItems.OrderByDescending(i => i.price).ToList(),
            "Random" => ShuffleItems(_inventoryItems),
            _ => _inventoryItems
        };
    }

    private List<ItemData> ShuffleItems(List<ItemData> itemList)
{
    List<ItemData> shuffled = new(itemList);

    for (int i = shuffled.Count - 1; i > 0; i--)
    {
        int j = UnityEngine.Random.Range(0, i + 1);

        (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
    }

    return shuffled;
}

    public void BuyItem(ItemData item)
    {
        if (Time.time - _lastInteractionTime < interactionCooldown)
        {
            Debug.LogWarning("Buy button is being clicked too fast.");
            return;
        }

        int purchasePrice = Globals.MoneyUtil.RoundEuro(item.price * Globals.MarketBuyPriceModifier);
        if (PlayerManager.Instance.GetPlayerBalance() >= purchasePrice)
        {
            PlayerManager.Instance.DeductMoney(purchasePrice);

            LogUtil.Economy.LogEuro(
                "MARKET_PURCHASE",
                -purchasePrice,
                PlayerManager.Instance.GetPlayerBalance(),
                $"{item.gun} | {item.name} {(item.isStatTrak ? "(Stattrak | ": "(")}{item.condition})"
            );

            InventoryManager.Instance.AddItemToInventory(item);
            AdjustItemPrice(item, 1);

            _lastInteractionTime = Time.time;
        }
        else
        {
            Debug.LogWarning("Not enough balance to purchase this item.");
        }
    }

    public void SellAllItems()
    {
        List<ItemData> _items = InventoryManager.Instance.GetAllItems();
        int inventoryValue = InventoryManager.Instance.CalculateInventoryValue(); 
        if (_items.Count > 0)
        {
            InventoryManager.Instance.ClearInventory(false);
            PlayerManager.Instance.AddMoney(inventoryValue);
            
            foreach (ItemData item in _items)
            {
                AdjustItemPrice(item, -1);
            }
            ShowSellTab();
        }
    }

    public void SellItem(ItemData item, GameObject itemPrefab)
    {
        if (Time.time - _lastInteractionTime < interactionCooldown)
        {
            Debug.LogWarning("Sell button is being clicked too fast.");
            return;
        }

        int sellingPrice = item.price;
        if (InventoryManager.Instance.HasItem(item))
        {
            InventoryManager.Instance.RemoveItemFromInventory(item);
            _inventoryItems.Remove(item);
            PlayerManager.Instance.AddMoney(sellingPrice);

            LogUtil.Economy.LogEuro(
                "MARKET_SALE",
                sellingPrice,
                PlayerManager.Instance.GetPlayerBalance(),
                $"{item.gun} | {item.name} {(item.isStatTrak ? "(Stattrak | ": "(")}{item.condition})"
            );

            Destroy(itemPrefab);
            int pageCount = Mathf.CeilToInt((float)_inventoryItems.Count / ItemsPerPage);
            currentPage = Mathf.Min(currentPage, Mathf.Max(0, pageCount - 1));
            DisplayPage(currentPage);
            AdjustItemPrice(item, -1);

            _lastInteractionTime = Time.time;
        }
        else
        {
            Debug.LogWarning("Item not available in inventory for sale.");
        }
    }

    private void AdjustItemPrice(ItemData item, int change)
    {
        item.demandScore += change;
        item.price = Mathf.Clamp(
            Globals.MoneyUtil.RoundEuro(item.basePrice * (1 + item.demandScore * FluctuationIntensity)),
            Globals.MoneyUtil.RoundEuro(item.basePrice * 0.75f),
            Globals.MoneyUtil.RoundEuro(item.basePrice * 1.5f));
        if (_marketplaceItems.TryGetValue(item, out MarketplaceItem marketplaceItem))
        {
            marketplaceItem.UpdatePrice(_isBuyingTabActive);
        }
    }

    private IEnumerator MarketFluctuationCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(MarketEventInterval);

            List<ItemData> itemsForFluctuation = _allItems.OrderBy(_ => UnityEngine.Random.value).Take(ItemsToFluctuate).ToList();

            foreach (ItemData item in itemsForFluctuation)
            {
                float randomFluctuation = UnityEngine.Random.Range(-FluctuationIntensity, FluctuationIntensity);
                int newPrice = Globals.MoneyUtil.RoundEuro(item.price * (1 + randomFluctuation));

                item.price = Mathf.Clamp(newPrice, Globals.MoneyUtil.RoundEuro(item.basePrice * 0.5f), Globals.MoneyUtil.RoundEuro(item.basePrice * 2f));
            }
            UpdateCurrentTab();
        }
    }

    private void ApplyFluctuation(ItemData item)
    {
        if (item.basePrice <= 0)
        {
            Debug.LogWarning($"{item.name} has a base price of zero or below. Skipping fluctuation.");
            return;
        }

        float randomFluctuation = UnityEngine.Random.Range(-FluctuationIntensity, FluctuationIntensity);
        int newPrice = Globals.MoneyUtil.RoundEuro(item.price * (1 + randomFluctuation));

        item.price = Mathf.Clamp(newPrice, Globals.MoneyUtil.RoundEuro(item.basePrice * 0.5f), Globals.MoneyUtil.RoundEuro(item.basePrice * 2f));

        if (item.price < 1)
        {
            item.price = item.basePrice;
        }
    }

    private IEnumerator DemandScoreDecayCoroutine()
    {
        while (true)
        {
            foreach (var item in _allItems)
            {
                if (Time.time - item.lastActivityTime >= ItemData.DemandDecayInterval && item.demandScore != 0)
                {
                    item.demandScore -= Mathf.CeilToInt(item.demandScore * ItemData.DecayRate);
                    
                    if (item.demandScore < 0) item.demandScore = 0;

                    item.price = Mathf.Clamp(
                        Globals.MoneyUtil.RoundEuro(item.basePrice * (1 + item.demandScore * FluctuationIntensity)),
                        Globals.MoneyUtil.RoundEuro(item.basePrice * 0.75f),
                        Globals.MoneyUtil.RoundEuro(item.basePrice * 1.5f));

                    if (_marketplaceItems.TryGetValue(item, out MarketplaceItem marketplaceItem))
                    {
                        marketplaceItem.UpdatePrice(_isBuyingTabActive);
                    }
                }
            }
            
            yield return new WaitForSeconds(ItemData.DemandDecayInterval);
        }
    }
}
