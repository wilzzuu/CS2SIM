using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

public class UpgraderManager : MonoBehaviour
{
    public static UpgraderManager Instance { get; private set; }
    
    public Transform inventoryCatalogGrid;
    public Transform upgradeCatalogGrid;
    public GameObject upgraderItemPrefab;
    public GameObject selectedUpgraderItemPrefab;
    public TMP_InputField searchInput;
    public Button searchBtn;
    public TMP_InputField priceInput;
    public Button searchPriceBtn;
    public TMP_Dropdown sortDropdown;
    public Toggle ascendingToggle;
    public TextMeshProUGUI inventoryValueText;
    public Button inventoryTabButton;
    public Button upgradeTabButton;
    public GameObject inventoryScrollView;
    public GameObject upgradeItemsScrollView;
    public Button[] multiplierButtons;

    private GameObject _selectedInventoryItemObj;
    private GameObject _selectedUpgradeItemObj;
    public Transform selectedInventoryItemContainer;
    public Transform selectedUpgradeItemContainer;
    public TextMeshProUGUI probabilityText;
    public TextMeshProUGUI multiplierText;
    public TextMeshProUGUI isSuccessText;
    public GameObject selectItemText1;
    public GameObject selectItemText2;
    public Button upgradeButton;

    private int _itemsLoaded;
    private bool _isLoadingMoreItems;

    private List<ItemData> _allItems = new List<ItemData>();
    private List<ItemData> _inventoryItems = new List<ItemData>();
    private bool _isInventoryTabActive = true;
    private ItemData _selectedInventoryItem;
    private ItemData _selectedUpgradeItem;
    private float _successProbability;

    private static readonly Dictionary<string, int> RarityOrder = new Dictionary<string, int>
    {
        {"Common", 1},
        {"Uncommon", 2},
        {"Rare", 3},
        {"Epic", 4},
        {"Legendary", 5}
    };

    private float[] _multipliers = { 1.5f, 2f, 5f, 10f, 20f };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        LoadAllGameItems();
        LoadInventoryItems();

        upgradeTabButton.interactable = false;
        ShowInventoryTab();

        SetMultiplierButtons(false);

        for (int i = 0; i < multiplierButtons.Length; i++)
        {
            float multiplier = _multipliers[i];
            multiplierButtons[i].onClick.AddListener(() => SelectRandomUpgradeItem(multiplier));
        }

        inventoryTabButton.onClick.AddListener(ShowInventoryTab);
        upgradeTabButton.onClick.AddListener(ShowUpgradeTab);

        ScrollRect scrollRect = (_isInventoryTabActive ? inventoryScrollView : upgradeItemsScrollView).GetComponent<ScrollRect>();
        scrollRect.onValueChanged.AddListener(OnScroll);

        searchBtn.onClick.AddListener(SearchItems);
        searchInput.onValueChanged.AddListener(delegate { ClearSearch(); });
        searchPriceBtn.onClick.AddListener(SearchPrices);
        priceInput.onValueChanged.AddListener(delegate { ClearPriceSearch(); });
        sortDropdown.onValueChanged.AddListener(delegate { SortItems(); });
        ascendingToggle.onValueChanged.AddListener(delegate { SortItems(); });
        
        upgradeButton.onClick.AddListener(AttemptUpgrade);
    }

    private void SetMultiplierButtons(bool enable)
    {
        foreach (Button button in multiplierButtons)
        {
            button.interactable = enable;
        }
    }
    
    void RefreshInventoryValue()
    {
        float inventoryValue = InventoryManager.Instance.CalculateInventoryValue();
        inventoryValueText.text = $"Inventory Value:        {inventoryValue:F2}";
    }

    private void UpdateCurrentTab()
    {
        DisplayItems();
    }

    void DisplayItems()
    {
        Transform currentCatalogGrid = _isInventoryTabActive ? inventoryCatalogGrid : upgradeCatalogGrid;
        List<ItemData> displayedItems = _isInventoryTabActive ? _inventoryItems : _allItems;

        if (currentCatalogGrid == null) return;
        
        if (!_isInventoryTabActive && _selectedInventoryItem != null)
        {
            displayedItems = _allItems.FindAll(upgradeItem => upgradeItem.Price > _selectedInventoryItem.Price);
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
            GameObject itemObj = Instantiate(upgraderItemPrefab, currentCatalogGrid);
            UpgraderItem upgraderItem = itemObj.GetComponent<UpgraderItem>();
            upgraderItem.Setup(displayedItems[i], _isInventoryTabActive);
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

    void OnScroll(Vector2 scrollPosition)
    {
        if (scrollPosition.y <= 0.1f && !_isLoadingMoreItems && _itemsLoaded < (_isInventoryTabActive ? _allItems.Count : _inventoryItems.Count))
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

    public void ShowInventoryTab()
    {
        _isInventoryTabActive = true;
        inventoryScrollView.SetActive(true);
        upgradeItemsScrollView.SetActive(false);
        _itemsLoaded = 0;
        RefreshInventoryValue();
        DisplayItems();
    }

    public void ShowUpgradeTab()
    {
        _isInventoryTabActive = false;
        inventoryScrollView.SetActive(false);
        upgradeItemsScrollView.SetActive(true);
        _itemsLoaded = 0;
        RefreshInventoryValue();
        DisplayItems();
    }

    public void SelectInventoryItem(ItemData item)
    {
        _selectedInventoryItem = item;
        upgradeTabButton.interactable = _selectedInventoryItem != null;
        SetMultiplierButtons(true);

        foreach (Transform child in selectedInventoryItemContainer.transform)
        {
            DestroyImmediate(child.gameObject);
        }

        _selectedInventoryItemObj = Instantiate(selectedUpgraderItemPrefab, selectedInventoryItemContainer);

        isSuccessText.text = "";
        PopulateItemPrefab(_selectedInventoryItemObj, item);
        selectItemText1.SetActive(false);
        UpdateUpgradeProbability();
    }

    public void SelectUpgradeItem(ItemData item)
    {
        _selectedUpgradeItem = item;

        foreach (Transform child in selectedUpgradeItemContainer.transform)
        {
            DestroyImmediate(child.gameObject);
        }

        _selectedUpgradeItemObj = Instantiate(selectedUpgraderItemPrefab, selectedUpgradeItemContainer);

        isSuccessText.text = "";
        PopulateItemPrefab(_selectedUpgradeItemObj, item);
        selectItemText2.SetActive(false);
        UpdateUpgradeProbability();
    }

    private void SelectRandomUpgradeItem(float multiplier)
    {
        if (_selectedInventoryItem == null) return;

        float targetPrice = _selectedInventoryItem.Price * multiplier;
        float minPrice = targetPrice * 0.8f;
        float maxPrice = targetPrice * 1.2f;

        List<ItemData> validItems = _allItems.FindAll(upgradeItem => upgradeItem.Price >= minPrice && upgradeItem.Price <= maxPrice);

        if (validItems.Count == 0)
        {
            validItems = _allItems.FindAll(upgradeItem => upgradeItem.Price > _selectedInventoryItem.Price);
        }

        if (validItems.Count > 0)
        {
            ItemData randomItem = validItems[Random.Range(0, validItems.Count)];
            SelectUpgradeItem(randomItem);
        }
    }

    private void PopulateItemPrefab(GameObject itemObj, ItemData item)
    {
        Image itemImage = itemObj.transform.Find("ItemImage").GetComponent<Image>();
        Image rarityImage = itemObj.transform.Find("RarityImage").GetComponent<Image>();
        TextMeshProUGUI nameText = itemObj.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI priceText = itemObj.transform.Find("PriceText").GetComponent<TextMeshProUGUI>();

        itemImage.sprite = Resources.Load<Sprite>($"ItemImages/{item.ID}");
        rarityImage.sprite = Resources.Load<Sprite>($"RarityImages/{item.Rarity}");
        nameText.text = item.Name;
        priceText.text = $"{item.Price:0.00}";
    }

    void UpdateUpgradeProbability()
    {
        if (_selectedInventoryItem != null && _selectedUpgradeItem != null)
        {
            _successProbability = _selectedInventoryItem.Price / _selectedUpgradeItem.Price;
            probabilityText.fontSize = 32;
            probabilityText.text = $"{_successProbability * 100:0.00}%";
            multiplierText.text = $"{_selectedUpgradeItem.Price / _selectedInventoryItem.Price:0.0}x";
        }
        else
        {
            probabilityText.fontSize = 20;
            probabilityText.text = "Select Both Items";
            multiplierText.text = "-";
        }
    }

    public void AttemptUpgrade()
    {
        if (_selectedInventoryItem == null || _selectedUpgradeItem == null) return;
        if (_selectedInventoryItem == _selectedUpgradeItem) return;

        float chance = Random.value;
        if (chance <= _successProbability)
        {
            InventoryManager.Instance.AddItemToInventory(_selectedUpgradeItem);
            CollectionManager.Instance.AddItemToCollection(_selectedUpgradeItem);
            InventoryManager.Instance.RemoveItemFromInventory(_selectedInventoryItem);
            isSuccessText.text = "Upgrade Successful!";
        }
        else
        {
            InventoryManager.Instance.RemoveItemFromInventory(_selectedInventoryItem);
            isSuccessText.text = "Upgrade Failed!";
        }

        LoadInventoryItems();

        _selectedInventoryItem = null;
        _selectedUpgradeItem = null;
        _selectedInventoryItemObj = null;
        _selectedUpgradeItemObj = null;
        selectItemText1.SetActive(true);
        selectItemText2.SetActive(true);
        probabilityText.text = "-";
        upgradeTabButton.interactable = false;
        ShowInventoryTab();
    }

    public void SortItems()
    {
        if (_isInventoryTabActive)
        {
            _inventoryItems = new List<ItemData>(_inventoryItems);
            string sortCriteria = sortDropdown.options[sortDropdown.value].text;
            bool ascending = ascendingToggle.isOn;
            _inventoryItems = SortInventoryItemsByCriteria(sortCriteria, ascending);
        }
        else
        {
            _allItems = new List<ItemData>(_allItems);
            string sortCriteria = sortDropdown.options[sortDropdown.value].text;
            bool ascending = ascendingToggle.isOn;
            _allItems = SortAllItemsByCriteria(sortCriteria, ascending);
        }
        UpdateCurrentTab();
    }
    public void SearchItems()
    {
        string query = searchInput.text.ToLower();

        if (string.IsNullOrWhiteSpace(query))
        {
            if (!_isInventoryTabActive)
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
            if (!_isInventoryTabActive)
            {
                _allItems = _allItems.Where(item =>
                    item.Name.ToLower().Contains(query) ||
                    item.ID.ToString().Contains(query)).ToList();
            }
            else
            {
                _inventoryItems = _inventoryItems.Where(item =>
                    item.Name.ToLower().Contains(query) ||
                    item.ID.ToString().Contains(query)).ToList();
            }
        }

        UpdateCurrentTab();
    }
    
    public void SearchPrices()
    {
        string query = priceInput.text;
        if (string.IsNullOrWhiteSpace(query))
        {
            if (!_isInventoryTabActive)
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
            if (!_isInventoryTabActive)
            {
                _allItems = _allItems.Where(item => item.Price <= itemPriceQuery).ToList();
            }
            else
            {
                _inventoryItems = _inventoryItems.Where(item => item.Price <= itemPriceQuery).ToList();
            }
        }
        UpdateCurrentTab();
    }

    private List<ItemData> SortAllItemsByCriteria(string criteria, bool ascending)
    {
        return criteria switch
        {
            "Item" => ascending
                ? _allItems.OrderBy(i => i.Item).ToList()
                : _allItems.OrderByDescending(i => i.Item).ToList(),
            "Rarity" => ascending
                ? _allItems.OrderBy(i => RarityOrder.ContainsKey(i.Rarity) ? RarityOrder[i.Rarity] : int.MaxValue).ToList()
                : _allItems.OrderByDescending(i => RarityOrder.ContainsKey(i.Rarity) ? RarityOrder[i.Rarity] : int.MinValue).ToList(),
            "Color" => ascending
                ? _allItems.OrderBy(i => i.Color).ToList()
                : _allItems.OrderByDescending(i => i.Color).ToList(),
            "Style" => ascending
                ? _allItems.OrderBy(i => i.Style).ToList()
                : _allItems.OrderByDescending(i => i.Style).ToList(),
            "Price" => ascending
                ? _allItems.OrderBy(i => i.Price).ToList()
                : _allItems.OrderByDescending(i => i.Price).ToList(),
            _ => _allItems
        };
    }

    private List<ItemData> SortInventoryItemsByCriteria(string criteria, bool ascending)
    {
        return criteria switch
        {
            "Item" => ascending
                ? _inventoryItems.OrderBy(i => i.Item).ToList()
                : _inventoryItems.OrderByDescending(i => i.Item).ToList(),
            "Rarity" => ascending
                ? _inventoryItems.OrderBy(i => RarityOrder.ContainsKey(i.Rarity) ? RarityOrder[i.Rarity] : int.MaxValue)
                    .ToList()
                : _inventoryItems
                    .OrderByDescending(i => RarityOrder.ContainsKey(i.Rarity) ? RarityOrder[i.Rarity] : int.MinValue)
                    .ToList(),
            "Color" => ascending
                ? _inventoryItems.OrderBy(i => i.Color).ToList()
                : _inventoryItems.OrderByDescending(i => i.Color).ToList(),
            "Style" => ascending
                ? _inventoryItems.OrderBy(i => i.Style).ToList()
                : _inventoryItems.OrderByDescending(i => i.Style).ToList(),
            "Price" => ascending
                ? _inventoryItems.OrderBy(i => i.Price).ToList()
                : _inventoryItems.OrderByDescending(i => i.Price).ToList(),
            _ => _inventoryItems
        };
    }
}
