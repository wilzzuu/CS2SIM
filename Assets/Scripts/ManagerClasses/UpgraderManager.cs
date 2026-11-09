using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgraderManager : MonoBehaviour
{
    public static UpgraderManager Instance { get; private set; }
    
    public Transform inventoryCatalogGrid;
    public Transform upgradeCatalogGrid;
    public GameObject upgraderItemPrefab;
    public TMP_InputField searchInput;
    public Button searchBtn;
    public TMP_InputField priceInput;
    public Button searchPriceBtn;
    public TMP_Dropdown sortDropdown;
    public Toggle ascendingToggle;
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
    
    public TextMeshProUGUI pageIndicatorText;
    private int _currentPage;
    private int _totalPages;
    private int _totalInventoryPages;
    const int ItemsPerPage = 24;

    private List<ItemData> _allItems = new List<ItemData>();
    private List<ItemData> _inventoryItems = new List<ItemData>();
    private bool _isInventoryTabActive = true;
    private ItemData _selectedInventoryItem;
    private ItemData _selectedUpgradeItem;
    private float _successProbability;

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
        
        _totalPages = Mathf.CeilToInt((float)_allItems.Count / ItemsPerPage);
        _totalInventoryPages = Mathf.CeilToInt((float)_inventoryItems.Count / ItemsPerPage);

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

    private void UpdateCurrentTab()
    {
        DisplayPage();
    }

    void DisplayPage()
    {
        Transform currentCatalogGrid = _isInventoryTabActive ? inventoryCatalogGrid : upgradeCatalogGrid;
        if (currentCatalogGrid == null) return;

        List<ItemData> displayedItems = GetDisplayedItems();
        int pageCount = _isInventoryTabActive ? _totalInventoryPages : _totalPages;
        pageIndicatorText.text = $"Page {_currentPage + 1} of {(pageCount == 0 ? 1 : pageCount)}";

        foreach (Transform child in currentCatalogGrid)
        {
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }

        int startIndex = _currentPage * ItemsPerPage;
        int endIndex = Mathf.Min(startIndex + ItemsPerPage, displayedItems.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            GameObject itemObj = Instantiate(upgraderItemPrefab, currentCatalogGrid);
            UpgraderItem upgraderItem = itemObj.GetComponent<UpgraderItem>();
            upgraderItem.Setup(displayedItems[i], _isInventoryTabActive);
        }
    }

    public void NextPage()
    {
        int itemsCount = GetDisplayedItems().Count;
        int pageCount = Mathf.CeilToInt((float)itemsCount / ItemsPerPage);

        if (_currentPage < pageCount - 1)
        {
            _currentPage++;
            DisplayPage();
        }
    }

    public void PreviousPage()
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            DisplayPage();
        }
    }
    
    public void ClearSearch()
    {
        if (string.IsNullOrWhiteSpace(searchInput.text))
        {
            _currentPage = 0;
            DisplayPage();
        }
    }

    public void ClearPriceSearch()
    {
        if (string.IsNullOrWhiteSpace(priceInput.text))
        {
            _currentPage = 0;
            DisplayPage();
        }
    }

    private List<ItemData> GetDisplayedItems()
    {
        IEnumerable<ItemData> items = _isInventoryTabActive ? _inventoryItems : _allItems;
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
            items = items.Where(item => item.price <= priceLimit);
        }

        if (!_isInventoryTabActive && _selectedInventoryItem != null)
        {
            items = items.Where(item => item.price > _selectedInventoryItem.price);
        }

        string criteria = sortDropdown.options[sortDropdown.value].text;
        return Globals.SortItems(items, criteria, ascendingToggle.isOn);
    }

    void LoadAllGameItems()
    {
        _allItems = Resources.LoadAll<ItemData>("ItemAssets").ToList();
    }

    void LoadInventoryItems()
    {
        _inventoryItems = InventoryManager.Instance.GetUnlockedItems();
    }

    public void ShowInventoryTab()
    {
        _isInventoryTabActive = true;
        inventoryScrollView.SetActive(true);
        upgradeItemsScrollView.SetActive(false);
        _currentPage = 0;
        DisplayPage();
    }

    public void ShowUpgradeTab()
    {
        _isInventoryTabActive = false;
        inventoryScrollView.SetActive(false);
        upgradeItemsScrollView.SetActive(true);
        _currentPage = 0;
        DisplayPage();
    }

    public void SelectInventoryItem(ItemData item)
    {
        if (item.isLocked)
        {
            Debug.LogWarning("Cannot select a locked item for upgrading.");
            return;
        }

        _selectedInventoryItem = item;
        upgradeTabButton.interactable = _selectedInventoryItem != null;
        SetMultiplierButtons(true);

        foreach (Transform child in selectedInventoryItemContainer.transform)
        {
            DestroyImmediate(child.gameObject);
        }

        _selectedInventoryItemObj = Instantiate(upgraderItemPrefab, selectedInventoryItemContainer);

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

        _selectedUpgradeItemObj = Instantiate(upgraderItemPrefab, selectedUpgradeItemContainer);

        isSuccessText.text = "";
        PopulateItemPrefab(_selectedUpgradeItemObj, item);
        selectItemText2.SetActive(false);
        UpdateUpgradeProbability();
    }

    private void SelectRandomUpgradeItem(float multiplier)
    {
        if (_selectedInventoryItem == null) return;

        float targetPrice = _selectedInventoryItem.price * multiplier;
        int minPrice = Globals.MoneyUtil.RoundEuro(targetPrice * 0.8f);
        int maxPrice = Globals.MoneyUtil.RoundEuro(targetPrice * 1.2f);

        List<ItemData> validItems = _allItems.FindAll(upgradeItem => upgradeItem.price >= minPrice && upgradeItem.price <= maxPrice);

        if (validItems.Count == 0)
        {
            validItems = _allItems.FindAll(upgradeItem => upgradeItem.price > _selectedInventoryItem.price);
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
        Image statTrakImage = itemObj.transform.Find("StatTrakImage").GetComponent<Image>();
        TextMeshProUGUI gunText = itemObj.transform.Find("GunText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI nameText = itemObj.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI priceText = itemObj.transform.Find("PriceText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI conditionText = itemObj.transform.Find("ConditionText").GetComponent<TextMeshProUGUI>();

        itemImage.sprite = Resources.Load<Sprite>($"ItemImages/{item.imageName}");
        rarityImage.sprite = Resources.Load<Sprite>($"RarityImages/{item.rarity}_ROULETTE_SLIM");
        
        if (item.isStatTrak)
        {
            statTrakImage.gameObject.SetActive(true);
        }
        else
        {
            statTrakImage.gameObject.SetActive(false);
        }
        
        gunText.text = item.gun;
        nameText.text = item.name;
        priceText.text = $"{Globals.FormatCurrencyStr(item.price, Globals.EURO_DECIMAL)}";
        conditionText.text = item.condition;
    }

    void UpdateUpgradeProbability()
    {
        if (_selectedInventoryItem != null && _selectedUpgradeItem != null)
        {
            _successProbability = (float)_selectedInventoryItem.price / _selectedUpgradeItem.price;
            probabilityText.fontSize = 32;
            probabilityText.text = $"{_successProbability * 100:0.00}%";
            multiplierText.text = $"{(float)_selectedUpgradeItem.price / _selectedInventoryItem.price:0.0}x";
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
        if (_selectedInventoryItem.isLocked)
        {
            Debug.LogWarning("Cannot upgrade a locked item.");
            return;
        }
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
}
