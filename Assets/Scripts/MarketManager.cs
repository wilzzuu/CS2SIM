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
    public Transform buyCatalogGrid;
    public Transform sellCatalogGrid;
    public GameObject marketItemPrefab;

    public Button buyTabButton;
    public Button sellTabButton;
    public GameObject buyScrollView;
    public GameObject sellScrollView;

    public TMP_InputField searchInput;
    public Button searchBtn;
    public TMP_InputField priceInput;
    public Button searchPriceBtn;
    public TMP_Dropdown sortDropdown;
    public Toggle ascendingToggle;
    public Toggle affordableItemsToggle;
    public TextMeshProUGUI inventoryValueText;

    private List<ItemData> _allItems = new List<ItemData>();
    private List<ItemData> _inventoryItems = new List<ItemData>();
    private readonly Dictionary<ItemData, MarketplaceItem> _marketplaceItems = new Dictionary<ItemData, MarketplaceItem>();

    private bool _isBuyingTabActive;

    private int _itemsLoaded;
    private bool _isLoadingMoreItems;

    private const float FluctuationIntensity = 0.1f;
    private const int ItemsToFluctuate = 20;
    private const float MarketEventInterval = 120f;
    private DateTime _lastUpdateTimestamp;
    public float interactionCooldown = 0.2f;
    private float _lastInteractionTime;
    private const string LastUpdateKey = "LastMarketUpdate";

    private static readonly Dictionary<string, int> RarityOrder = new Dictionary<string, int>
    {
        {"MIL_SPEC", 1},
        {"RESTRICTED", 2},
        {"CLASSIFIED", 3},
        {"COVERT", 4},
        {"SPECIAL", 5}
    };

    private static readonly Dictionary<string, int> ConditionOrder = new Dictionary<string, int>
    {
        {"Battle-Scarred",1},
        {"Well-Worn",2},
        {"Field-Tested",3},
        {"Minimal Wear",4},
        {"Factory New",5}
    };

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

        buyTabButton.onClick.AddListener(ShowBuyTab);
        sellTabButton.onClick.AddListener(ShowSellTab);

        ScrollRect scrollRect = (_isBuyingTabActive ? buyScrollView : sellScrollView).GetComponent<ScrollRect>();
        scrollRect.onValueChanged.AddListener(OnScroll);

        searchBtn.onClick.AddListener(SearchItems);
        searchInput.onValueChanged.AddListener(delegate { ClearSearch(); });
        searchPriceBtn.onClick.AddListener(SearchPrices);
        priceInput.onValueChanged.AddListener(delegate { ClearPriceSearch(); });

        sortDropdown.onValueChanged.AddListener(delegate { SortItems(); });
        ascendingToggle.onValueChanged.AddListener(delegate { SortItems(); });
        affordableItemsToggle.onValueChanged.AddListener(delegate { DisplayItems(); });
        InventoryManager.Instance.OnInventoryValueChanged += RefreshInventoryValue;
        PlayerManager.Instance.OnBalanceChanged += RefreshBalanceFilter;

        LoadLastUpdateTimestamp();
        ApplyRealTimeFluctuations();
        StartCoroutine(MarketFluctuationCoroutine());
        StartCoroutine(DemandScoreDecayCoroutine());
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private void UpdateCurrentTab()
    {
        DisplayItems();
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

    void OnScroll(Vector2 scrollPosition)
    {
        if (scrollPosition.y <= 0.1f && !_isLoadingMoreItems && _itemsLoaded < (_isBuyingTabActive ? _allItems.Count : _inventoryItems.Count))
        {
            _isLoadingMoreItems = true;
            DisplayItems();
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
        RefreshInventoryValue();
        DisplayItems();
    }

    public void ShowSellTab()
    {
        _isBuyingTabActive = false;
        buyScrollView.SetActive(false);
        sellScrollView.SetActive(true);
        RefreshInventoryValue();
        LoadInventoryItems();
        DisplayItems();
    }

    void RefreshInventoryValue()
    {
        float inventoryValue = InventoryManager.Instance.CalculateInventoryValue();
        inventoryValueText.text = $"Inventory Value: {inventoryValue:F2}€";
    }

    // ReSharper disable Unity.PerformanceAnalysis
    void RefreshBalanceFilter()
    {
        if (_isBuyingTabActive && affordableItemsToggle.isOn)
        {
            DisplayItems();
        }
    }

    void DisplayItems()
    {
        Transform currentCatalogGrid = _isBuyingTabActive ? buyCatalogGrid : sellCatalogGrid;
        List<ItemData> displayedItems = _isBuyingTabActive ? _allItems : _inventoryItems;

        if (currentCatalogGrid == null) return;
        
        if (_isBuyingTabActive && affordableItemsToggle.isOn)
        {
            float playerBalance = PlayerManager.Instance.GetPlayerBalance();
            displayedItems = displayedItems.Where(item => item.price * 1.15f <= playerBalance).ToList();
        }

        foreach (Transform child in currentCatalogGrid)
        {
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }
        
        for (int i = 0; i < displayedItems.Count; i++)
        {
            GameObject itemObj = Instantiate(marketItemPrefab, currentCatalogGrid);
            MarketplaceItem marketplaceItem = itemObj.GetComponent<MarketplaceItem>();
            marketplaceItem.Setup(displayedItems[i], _isBuyingTabActive);
            _marketplaceItems[displayedItems[i]] = marketplaceItem;
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
            bool ascending = ascendingToggle.isOn;

            _allItems = SortAllItemsByCriteria(sortCriteria, ascending);
        }
        else
        {
            _inventoryItems = new List<ItemData>(_inventoryItems);
            string sortCriteria = sortDropdown.options[sortDropdown.value].text;
            bool ascending = ascendingToggle.isOn;
            
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

        if (float.TryParse(query, out float itemPriceQuery))
        {
            if (_isBuyingTabActive)
            {
                _allItems = _allItems.Where(item => item.price * 1.15f <= itemPriceQuery).ToList();
            }
            else
            {
                _inventoryItems = _inventoryItems.Where(item => item.price * 0.85f <= itemPriceQuery).ToList();
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
                    .OrderBy(i => RarityOrder.ContainsKey(i.rarity) ? RarityOrder[i.rarity] : int.MaxValue)
                    .ToList()
                : _allItems
                    .OrderByDescending(i => RarityOrder.ContainsKey(i.rarity) ? RarityOrder[i.rarity] : int.MinValue)
                    .ToList(),
            "Gun" => ascending
                ? _allItems.OrderBy(i => i.gun).ToList()
                : _allItems.OrderByDescending(i => i.gun).ToList(),
            "Condition" => ascending
                ? _allItems
                    .OrderBy(i => ConditionOrder.ContainsKey(i.condition) ? RarityOrder[i.condition] : int.MaxValue)
                    .ToList()
                : _allItems
                    .OrderByDescending(i => ConditionOrder.ContainsKey(i.condition) ? RarityOrder[i.condition] : int.MinValue)
                    .ToList(),
            "Price" => ascending
                ? _allItems.OrderBy(i => i.price).ToList()
                : _allItems.OrderByDescending(i => i.price).ToList(),
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
                    .OrderBy(i => RarityOrder.ContainsKey(i.rarity) ? RarityOrder[i.rarity] : int.MaxValue)
                    .ToList()
                : _inventoryItems
                    .OrderByDescending(i => RarityOrder.ContainsKey(i.rarity) ? RarityOrder[i.rarity] : int.MinValue)
                    .ToList(),
            "Gun" => ascending
                ? _inventoryItems.OrderBy(i => i.gun).ToList()
                : _inventoryItems.OrderByDescending(i => i.gun).ToList(),
            "Condition" => ascending
                ? _inventoryItems
                    .OrderBy(i => ConditionOrder.ContainsKey(i.condition) ? RarityOrder[i.condition] : int.MaxValue)
                    .ToList()
                : _inventoryItems
                    .OrderByDescending(i => ConditionOrder.ContainsKey(i.condition) ? RarityOrder[i.condition] : int.MinValue)
                    .ToList(),
            "Price" => ascending
                ? _inventoryItems.OrderBy(i => i.price).ToList()
                : _inventoryItems.OrderByDescending(i => i.price).ToList(),
            _ => _inventoryItems
        };
    }

    public void BuyItem(ItemData item)
    {
        if (Time.time - _lastInteractionTime < interactionCooldown)
        {
            Debug.LogWarning("Buy button is being clicked too fast.");
            return;
        }

        float purchasePrice = item.price * 1.15f;
        if (PlayerManager.Instance.GetPlayerBalance() >= purchasePrice)
        {
            PlayerManager.Instance.DeductCurrency(purchasePrice);
            InventoryManager.Instance.AddItemToInventory(item);
            AdjustItemPrice(item, 1);

            _lastInteractionTime = Time.time;
        }
        else
        {
            Debug.LogWarning("Not enough balance to purchase this item.");
        }
    }

    public void SellItem(ItemData item, GameObject itemPrefab)
    {
        if (Time.time - _lastInteractionTime < interactionCooldown)
        {
            Debug.LogWarning("Sell button is being clicked too fast.");
            return;
        }

        float sellingPrice = item.price * 0.85f;
        if (InventoryManager.Instance.HasItem(item))
        {
            InventoryManager.Instance.RemoveItemFromInventory(item);
            PlayerManager.Instance.AddCurrency(sellingPrice);
            Destroy(itemPrefab);
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
        item.price = Mathf.Clamp(item.basePrice * (1 + item.demandScore * FluctuationIntensity), item.basePrice * 0.75f, item.basePrice * 1.5f);
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
                float newPrice = item.price * (1 + randomFluctuation);

                item.price = Mathf.Clamp(newPrice, item.basePrice * 0.5f, item.basePrice * 2f);
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
        float newPrice = item.price * (1 + randomFluctuation);

        item.price = Mathf.Clamp(newPrice, item.basePrice * 0.5f, item.basePrice * 2f);

        if (item.price < 0.01f)
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

                    item.price = Mathf.Clamp(item.basePrice * (1 + item.demandScore * FluctuationIntensity), item.basePrice * 0.75f, item.basePrice * 1.5f);

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
