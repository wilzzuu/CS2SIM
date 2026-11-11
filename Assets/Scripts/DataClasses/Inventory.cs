using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryScreen : MonoBehaviour, IItemFilterScreen
{
    public GameObject inventorySlotPrefab;
    public Transform inventoryGrid;
    public TextMeshProUGUI pageIndicatorText;
    public TMP_Dropdown sortDropdown;
    public Toggle ascendingToggle;
    public TMP_InputField searchInput;
    public Button searchBtn;
    public TMP_InputField priceInput;
    public Button searchPriceBtn;
    public int itemsPerPage = 40;
    private int _currentPage;

    private List<ItemData> _items = new List<ItemData>();
    private readonly ItemFilter _filter = new ItemFilter();

    void Start()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager instance is not set.");
            return;
        }

        if (searchBtn != null)
        {
            searchBtn.onClick.AddListener(SearchItems);
        }
        if (searchInput != null)
        {
            searchInput.onValueChanged.AddListener(OnSearchInputChanged);
        }
        if (searchPriceBtn != null)
        {
            searchPriceBtn.onClick.AddListener(SearchPrices);
        }
        if (priceInput != null)
        {
            priceInput.onValueChanged.AddListener(OnPriceInputChanged);
        }

        _items = sortDropdown == null
            ? InventoryManager.Instance.GetInventoryItems()
            : InventoryManager.Instance.GetAllItems();
        if (sortDropdown != null)
        {
            sortDropdown.onValueChanged.AddListener(HandleSortChanged);
            if (ascendingToggle != null)
            {
                ascendingToggle.onValueChanged.AddListener(HandleSortOrderChanged);
            }

            SortItems();
            return;
        }

        if (_items.Count == 0)
        {
            return;
        }

        DisplayCurrentPage();
    }

    void OnDestroy()
    {
        if (searchBtn != null)
        {
            searchBtn.onClick.RemoveListener(SearchItems);
        }
        if (searchInput != null)
        {
            searchInput.onValueChanged.RemoveListener(OnSearchInputChanged);
        }
        if (searchPriceBtn != null)
        {
            searchPriceBtn.onClick.RemoveListener(SearchPrices);
        }
        if (priceInput != null)
        {
            priceInput.onValueChanged.RemoveListener(OnPriceInputChanged);
        }
        if (sortDropdown != null)
        {
            sortDropdown.onValueChanged.RemoveListener(HandleSortChanged);
        }

        if (ascendingToggle != null)
        {
            ascendingToggle.onValueChanged.RemoveListener(HandleSortOrderChanged);
        }
    }

    private void HandleSortChanged(int _)
    {
        SortItems();
    }

    private void HandleSortOrderChanged(bool _)
    {
        SortItems();
    }

    public void SortItems()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager instance is not set.", this);
            return;
        }

        if (sortDropdown == null || sortDropdown.value < 0 || sortDropdown.value >= sortDropdown.options.Count)
        {
            Debug.LogError("Inventory sort dropdown is not assigned or has no selected option.", this);
            return;
        }

        string criteria = sortDropdown.options[sortDropdown.value].text;
        if (criteria == "Random")
        {
            Globals.RandomizeItemSortOrder();
        }

        RefreshItems();
    }

    public void SearchItems()
    {
        RefreshSearch();
    }

    public void SearchPrices()
    {
        RefreshSearch();
    }

    public void ClearSearch()
    {
        if (searchInput == null || string.IsNullOrWhiteSpace(searchInput.text))
        {
            RefreshSearch();
        }
    }

    public void ClearPriceSearch()
    {
        if (priceInput == null || string.IsNullOrWhiteSpace(priceInput.text))
        {
            RefreshSearch();
        }
    }

    private void OnSearchInputChanged(string _)
    {
        ClearSearch();
    }

    private void OnPriceInputChanged(string _)
    {
        ClearPriceSearch();
    }

    private void RefreshSearch()
    {
        _currentPage = 0;
        RefreshItems();
    }

    public void OpenFilters()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("Cannot open item filters: InventoryManager instance is not set.", this);
            return;
        }

        ItemFilterPopup.Open(inventoryGrid, _filter,
            InventoryManager.Instance.GetAllItems(), RefreshItems);
    }

    private void RefreshItems()
    {
        IEnumerable<ItemData> items = _filter.Apply(InventoryManager.Instance.GetAllItems());
        string searchQuery = searchInput == null ? string.Empty : searchInput.text.Trim();
        if (!string.IsNullOrEmpty(searchQuery))
        {
            items = items.Where(item =>
                (item.name != null && item.name.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (item.id != null && item.id.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        if (priceInput != null && Globals.TryParseEuroInput(priceInput.text, out int priceLimit))
        {
            items = items.Where(item => item.price <= priceLimit);
        }

        if (sortDropdown != null && sortDropdown.value >= 0 && sortDropdown.value < sortDropdown.options.Count)
        {
            bool ascending = ascendingToggle == null || ascendingToggle.isOn;
            _items = Globals.SortItems(items, sortDropdown.options[sortDropdown.value].text, ascending);
        }
        else
        {
            _items = new List<ItemData>(items);
        }

        _currentPage = 0;
        DisplayCurrentPage();
    }

    void DisplayCurrentPage()
    {
        foreach (Transform child in inventoryGrid)
        {
            Destroy(child.gameObject);
        }

        int pageCount = Mathf.CeilToInt((float)_items.Count / itemsPerPage);
        pageIndicatorText.text = $"Page {_currentPage + 1} of {(pageCount == 0 ? 1 : pageCount)}";

        int itemIndexOffset = _currentPage * itemsPerPage;
        int endIndex = Mathf.Min(itemIndexOffset + itemsPerPage, _items.Count);

        for (int i = itemIndexOffset; i < endIndex; i++)
        {
            GameObject slot = Instantiate(inventorySlotPrefab, inventoryGrid);
            ItemData item = _items[i];
            InventoryItemLockToggle lockToggle = slot.GetComponent<InventoryItemLockToggle>();
            if (lockToggle == null)
            {
                Debug.LogError("Inventory slot prefab is missing InventoryItemLockToggle.");
                continue;
            }
            lockToggle.Setup(item);

            Image itemImage = slot.transform.Find("ItemImage")?.GetComponent<Image>();
            Image rarityImage = slot.transform.Find("RarityImage")?.GetComponent<Image>();
            Image statTrakImage = slot.transform.Find("StatTrakImage")?.GetComponent<Image>();
            TextMeshProUGUI conditionText = slot.transform.Find("ConditionText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI floatText = slot.transform.Find("FloatText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI patternText = slot.transform.Find("PatternText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI gunText = slot.transform.Find("GunText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI nameText = slot.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI priceText = slot.transform.Find("PriceText")?.GetComponent<TextMeshProUGUI>();

            if (itemImage != null)
            {
                string imagePath = $"ItemImages/{item.imageName}";
                Sprite itemSprite = Resources.Load<Sprite>(imagePath);
                if (itemSprite != null)
                {
                    itemImage.sprite = itemSprite;
                }
                else
                {
                    Debug.LogWarning($"Failed to load item image for path: {imagePath}");
                }
            }

            if (rarityImage != null)
            {
                string rarityPath = "RarityImages/" + item.rarity + "_BOTTOM";
                Sprite raritySprite = Resources.Load<Sprite>(rarityPath);
                if (raritySprite != null)
                {
                    rarityImage.sprite = raritySprite;
                }
                else
                {
                    Debug.LogWarning($"Failed to load rarity image for path: {rarityPath}");
                }
            }

            if (conditionText != null)
            {
                conditionText.text = item.condition;
            }

            if (floatText != null)
            {
                floatText.text = $"{item.wearFloat}";
            }

            if (patternText != null)
            {
                patternText.text = $"{item.pattern}";
            }
            
            if (statTrakImage != null)
            {
                if (item.isStatTrak)
                {
                    statTrakImage.gameObject.SetActive(true);
                }
                else
                {
                    statTrakImage.gameObject.SetActive(false);
                }
            }
            
            gunText.text = item.gun;
            nameText.text = item.name;
            priceText.text = $"{Globals.FormatCurrencyStr(item.price, Globals.EURO_DECIMAL)}€";
        }
    }

    public void NextPage()
    {
        if ((_currentPage + 1) * itemsPerPage < _items.Count)
        {
            _currentPage++;
            DisplayCurrentPage();
        }
    }

    public void PreviousPage()
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            DisplayCurrentPage();
        }
    }
}
