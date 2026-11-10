using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections;
using System;
using System.Globalization;

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

    public TextMeshProUGUI pageIndicatorText;

    private List<ItemData> _allItems = new List<ItemData>();
    private List<ItemData> _inventoryItems = new List<ItemData>();
    private readonly Dictionary<ItemData, MarketplaceItem> _marketplaceItems = new Dictionary<ItemData, MarketplaceItem>();

    private int _currentPage;
    private int _totalPages;

    const int ItemsPerPage = 30;

    private bool _isBuyingTabActive;

    private const float FluctuationIntensity = 0.025f;
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
        filterByBalanceToggle.onValueChanged.AddListener(delegate
        {
            _currentPage = 0;
            DisplayPage(_currentPage);
        });
        _totalPages = Mathf.CeilToInt((float)GetDisplayedItems().Count / ItemsPerPage);

        PlayerManager.Instance.OnBalanceChanged += RefreshBalanceFilter;

        LoadLastUpdateTimestamp();
        ApplyRealTimeFluctuations();
        StartCoroutine(MarketFluctuationCoroutine());
        StartCoroutine(DemandScoreDecayCoroutine());
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private void UpdateCurrentTab()
    {
        DisplayPage(_currentPage);
    }

    void LoadLastUpdateTimestamp()
    {
        if (!PlayerPrefs.HasKey(LastUpdateKey))
        {
            _lastUpdateTimestamp = DateTime.UtcNow;
            SaveLastUpdateTimestamp();
            return;
        }

        string lastUpdateString = PlayerPrefs.GetString(LastUpdateKey);
        if (!DateTime.TryParse(
                lastUpdateString,
                CultureInfo.CurrentCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out _lastUpdateTimestamp)
            && !DateTime.TryParse(
                lastUpdateString,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out _lastUpdateTimestamp))
        {
            Debug.LogWarning("Invalid saved market update timestamp; resetting its schedule.");
            _lastUpdateTimestamp = DateTime.UtcNow;
        }

        _lastUpdateTimestamp = _lastUpdateTimestamp.ToUniversalTime();
        SaveLastUpdateTimestamp();
    }

    private void SaveLastUpdateTimestamp()
    {
        PlayerPrefs.SetString(LastUpdateKey, Globals.FormatUtcDateTime(_lastUpdateTimestamp));
        PlayerPrefs.Save();
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
            SaveLastUpdateTimestamp();
        }
    }

    void LoadAllGameItems()
    {
        _allItems = Resources.LoadAll<ItemData>("ItemAssets").ToList();
    }

    void LoadInventoryItems()
    {
        _inventoryItems = InventoryManager.Instance.GetUnlockedItems();
    }

    public void ShowBuyTab()
    {
        _isBuyingTabActive = true;
        buyScrollView.SetActive(true);
        sellScrollView.SetActive(false);
        sellAllButton.gameObject.SetActive(false);

        _currentPage = 0;
        DisplayPage(_currentPage);
    }

    public void ShowSellTab()
    {
        _isBuyingTabActive = false;
        buyScrollView.SetActive(false);
        sellScrollView.SetActive(true);
        sellAllButton.gameObject.SetActive(true);
        
        LoadInventoryItems();
        
        _currentPage = 0;
        DisplayPage(_currentPage);
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
        if (currentCatalogGrid == null) return;

        List<ItemData> displayedItems = GetDisplayedItems();
        _totalPages = Mathf.CeilToInt((float)displayedItems.Count / ItemsPerPage);
        _currentPage = _totalPages == 0 ? 0 : Mathf.Clamp(page, 0, _totalPages - 1);
        page = _currentPage;

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

        pageIndicatorText.text = $"Page {_currentPage + 1} of {Mathf.Max(_totalPages, 1)}";
    }

    public void NextPage()
    {
        int itemsCount = GetDisplayedItems().Count;
        _totalPages = Mathf.CeilToInt((float)itemsCount / ItemsPerPage);

        if (_currentPage < _totalPages - 1)
        {
            _currentPage++;
            DisplayPage(_currentPage);
        }
    }

    public void PreviousPage()
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            DisplayPage(_currentPage);
        }
    }

    public void ClearSearch()
    {
        if (string.IsNullOrWhiteSpace(searchInput.text))
        {
            _currentPage = 0;
            DisplayPage(_currentPage);
        }
    }

    public void ClearPriceSearch()
    {
        if (string.IsNullOrWhiteSpace(priceInput.text))
        {
            _currentPage = 0;
            DisplayPage(_currentPage);
        }
    }

    public void SortItems()
    {
        if (sortDropdown.options[sortDropdown.value].text == "Random")
        {
            Globals.RandomizeItemSortOrder();
        }

        _currentPage = 0;
        UpdateCurrentTab();
    }

    public void SearchItems()
    {
        _currentPage = 0;
        UpdateCurrentTab();
    }

    public void SearchPrices()
    {
        _currentPage = 0;
        UpdateCurrentTab();
    }

    private List<ItemData> GetDisplayedItems()
    {
        IEnumerable<ItemData> items = _isBuyingTabActive ? _allItems : _inventoryItems;
        string searchQuery = searchInput.text.Trim();
        if (!string.IsNullOrEmpty(searchQuery))
        {
            searchQuery = searchQuery.ToLowerInvariant();
            items = items.Where(item =>
                item.name.ToLowerInvariant().Contains(searchQuery) ||
                (item.id != null && item.id.ToLowerInvariant().Contains(searchQuery)));
        }

        if (Globals.TryParseEuroInput(priceInput.text, out int priceLimit))
        {
            items = _isBuyingTabActive
                ? items.Where(item => item.price * Globals.MarketBuyPriceModifier <= priceLimit)
                : items.Where(item => item.price <= priceLimit);
        }

        if (_isBuyingTabActive && filterByBalanceToggle.isOn)
        {
            int playerBalance = PlayerManager.Instance.GetPlayerBalance();
            items = items.Where(item => item.price * Globals.MarketBuyPriceModifier <= playerBalance);
        }

        string criteria = sortDropdown.options[sortDropdown.value].text;
        return Globals.SortItems(items, criteria, toggleSortingOrder.isOn);
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
            UpdateCurrentTab();
        }
        else
        {
            Debug.LogWarning("Not enough balance to purchase this item.");
        }
    }

    public void SellAllItems()
    {
        List<ItemData> _items = InventoryManager.Instance.GetUnlockedItems();
        int inventoryValue = _items.Sum(item => item.price);
        if (_items.Count > 0)
        {
            InventoryManager.Instance.RemoveItemsFromInventory(_items);
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
        if (item.isLocked)
        {
            Debug.LogWarning("Cannot sell a locked item.");
            return;
        }

        if (Time.time - _lastInteractionTime < interactionCooldown)
        {
            Debug.LogWarning("Sell button is being clicked too fast.");
            return;
        }

        int sellingPrice = item.price;
        if (InventoryManager.Instance.HasItem(item))
        {
            InventoryManager.Instance.RemoveItemFromInventory(item);
            PlayerManager.Instance.AddMoney(sellingPrice);

            LogUtil.Economy.LogEuro(
                "MARKET_SALE",
                sellingPrice,
                PlayerManager.Instance.GetPlayerBalance(),
                $"{item.gun} | {item.name} {(item.isStatTrak ? "(Stattrak | ": "(")}{item.condition})"
            );

            AdjustItemPrice(item, -1);
            LoadInventoryItems();
            Destroy(itemPrefab);
            DisplayPage(_currentPage);

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
            yield return new WaitForSecondsRealtime(MarketEventInterval);

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
