using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ItemFilterTests
{
    private readonly List<ItemData> _items = new List<ItemData>();

    [TearDown]
    public void TearDown()
    {
        foreach (ItemData item in _items) Object.DestroyImmediate(item);
        _items.Clear();
    }

    private ItemData CreateItem(string condition, string rarity, bool statTrak, string type, string gun, int price)
    {
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        item.condition = condition;
        item.rarity = rarity;
        item.isStatTrak = statTrak;
        item.type = type;
        item.gun = gun;
        item.price = price;
        _items.Add(item);
        return item;
    }

    [Test]
    public void EmptyFilterMatchesAllItemsAndClearRestoresThem()
    {
        ItemData item = CreateItem("Factory New", "Covert", true, "Rifle", "AK-47", 50);
        ItemFilter filter = new ItemFilter();
        Assert.That(filter.Matches(item), Is.True);
        filter.Guns.Add("AWP");
        Assert.That(filter.Matches(item), Is.False);
        filter.Clear();
        Assert.That(filter.Matches(item), Is.True);
    }

    [Test]
    public void MultipleValuesAreOrWithinCategoriesAndAndAcrossCategories()
    {
        ItemData first = CreateItem("Factory New", "Covert", true, "Rifle", "AK-47", 50);
        ItemData second = CreateItem("Minimal Wear", "Covert", true, "Rifle", "AWP", 20);
        ItemData wrongStatus = CreateItem("Factory New", "Covert", false, "Rifle", "AK-47", 10);
        ItemData wrongRarity = CreateItem("Factory New", "Restricted", true, "Rifle", "AK-47", 30);
        ItemFilter filter = new ItemFilter();
        filter.Conditions.UnionWith(new[] { "Factory New", "Minimal Wear" });
        filter.Guns.UnionWith(new[] { "AK-47", "AWP" });
        filter.Rarities.Add("Covert");
        filter.Types.Add("Rifle");
        filter.StatTrak.Add(true);

        CollectionAssert.AreEqual(new[] { first, second }, filter.Apply(_items).ToList());
        Assert.That(filter.Matches(wrongStatus), Is.False);
        Assert.That(filter.Matches(wrongRarity), Is.False);
        filter.StatTrak.Add(false);
        Assert.That(filter.Matches(wrongStatus), Is.True);
    }

    [Test]
    public void FilteringAndSortingComposeInEitherOrder()
    {
        ItemData expensive = CreateItem("Factory New", "Covert", false, "Rifle", "AK-47", 50);
        ItemData cheap = CreateItem("Factory New", "Covert", false, "Rifle", "AWP", 20);
        CreateItem("Minimal Wear", "Covert", false, "Rifle", "AWP", 10);
        ItemFilter filter = new ItemFilter();
        filter.Conditions.Add("Factory New");

        CollectionAssert.AreEqual(new[] { cheap, expensive }, Globals.SortItems(filter.Apply(_items), "Price", true));
        CollectionAssert.AreEqual(new[] { expensive, cheap }, filter.Apply(Globals.SortItems(_items, "Price", false)));
    }

    [Test]
    public void PopupCreatesAndAppliesIndependentOptions()
    {
        CreateItem("Factory New", "Covert", true, "Rifle", "AK-47", 50);
        CreateItem("Minimal Wear", "Restricted", false, "Pistol", "Glock-18", 20);
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        try
        {
            ItemFilter filter = new ItemFilter();
            int changes = 0;
            ItemFilterPopup.Open(canvasObject.transform, filter, _items, () => changes++);
            ItemFilterPopup popup = canvasObject.GetComponentInChildren<ItemFilterPopup>();
            Assert.That(popup, Is.Not.Null);
            Toggle[] toggles = popup.GetComponentsInChildren<Toggle>(true);
            Toggle factoryNew = toggles.Single(toggle => toggle.name == "Factory New");
            Toggle glock = toggles.Single(toggle => toggle.name == "Glock-18");
            factoryNew.isOn = true;
            glock.isOn = true;
            Assert.That(filter.Conditions, Does.Contain("Factory New"));
            Assert.That(filter.Guns, Does.Contain("Glock-18"));
            Assert.That(changes, Is.EqualTo(2));
            Assert.That(filter.Apply(_items), Is.Empty);
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void RouletteKeepsHiddenSelectionWhenSortingFilteredInventory()
    {
        ItemData selected = CreateItem("Factory New", "Covert", false, "Rifle", "AK-47", 50);
        ItemData visible = CreateItem("Factory New", "Covert", false, "Rifle", "AWP", 20);
        GameObject root = new GameObject("Roulette", typeof(RectTransform));
        try
        {
            RouletteManager manager = root.AddComponent<RouletteManager>();
            manager.inventoryCatalogGrid = new GameObject("Inventory", typeof(RectTransform)).transform;
            manager.inventoryCatalogGrid.SetParent(root.transform, false);
            manager.selectedItemsGrid = new GameObject("Selected", typeof(RectTransform)).transform;
            manager.selectedItemsGrid.SetParent(root.transform, false);
            manager.pageIndicatorText = new GameObject("Page", typeof(RectTransform), typeof(TextMeshProUGUI))
                .GetComponent<TextMeshProUGUI>();
            manager.pageIndicatorText.transform.SetParent(root.transform, false);
            manager.sortDropdown = new GameObject("Sort", typeof(RectTransform), typeof(TMP_Dropdown))
                .GetComponent<TMP_Dropdown>();
            manager.sortDropdown.transform.SetParent(root.transform, false);
            manager.sortDropdown.options = new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("Price") };
            manager.inventoryItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RouletteSelectItemPrefab.prefab");
            Assert.That(manager.inventoryItemPrefab, Is.Not.Null);

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(RouletteManager).GetField("_playerItems", flags).SetValue(manager, new List<ItemData> { selected, visible });
            ItemFilter filter = (ItemFilter)typeof(RouletteManager).GetField("_filter", flags).GetValue(manager);
            filter.Guns.Add("AWP");
            List<int> selection = (List<int>)typeof(RouletteManager).GetField("_selectedPlayerItemIndices", flags).GetValue(manager);
            selection.Add(0);

            manager.SortItems();

            List<ItemData> sorted = (List<ItemData>)typeof(RouletteManager).GetField("_playerItems", flags).GetValue(manager);
            CollectionAssert.AreEqual(new[] { visible, selected }, sorted);
            CollectionAssert.AreEqual(new[] { 1 }, selection);
            List<int> visibleIndices = (List<int>)typeof(RouletteManager)
                .GetMethod("GetVisiblePlayerItemIndices", flags).Invoke(manager, null);
            CollectionAssert.AreEqual(new[] { 0 }, visibleIndices);
            Assert.That(manager.inventoryCatalogGrid.childCount, Is.EqualTo(1));
            Assert.That(manager.selectedItemsGrid.childCount, Is.EqualTo(1));
            Assert.That(manager.pageIndicatorText.text, Is.EqualTo("Page 1 of 1"));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void MarketCombinesFiltersWithSearchPriceAndSortingOnBothTabs()
    {
        ItemData expensive = CreateItem("Factory New", "Covert", false, "Rifle", "AK-47", 500);
        ItemData cheap = CreateItem("Factory New", "Covert", false, "Rifle", "AWP", 200);
        ItemData excluded = CreateItem("Minimal Wear", "Covert", false, "Rifle", "AWP", 100);
        expensive.name = cheap.name = excluded.name = "Example";
        GameObject root = new GameObject("Market", typeof(RectTransform));
        try
        {
            MarketManager manager = root.AddComponent<MarketManager>();
            manager.searchInput = CreateChild<TMP_InputField>(root.transform, "Search");
            manager.searchInput.text = "example";
            manager.priceInput = CreateChild<TMP_InputField>(root.transform, "Price");
            manager.priceInput.text = "10";
            manager.filterByBalanceToggle = CreateChild<Toggle>(root.transform, "Balance");
            manager.filterByBalanceToggle.isOn = false;
            manager.sortDropdown = CreateChild<TMP_Dropdown>(root.transform, "Sort");
            manager.sortDropdown.options = new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("Price") };
            manager.toggleSortingOrder = CreateChild<Toggle>(root.transform, "Ascending");
            manager.toggleSortingOrder.isOn = true;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            List<ItemData> items = new List<ItemData> { expensive, excluded, cheap };
            typeof(MarketManager).GetField("_allItems", flags).SetValue(manager, items);
            typeof(MarketManager).GetField("_inventoryItems", flags).SetValue(manager, items);
            ItemFilter filter = (ItemFilter)typeof(MarketManager).GetField("_filter", flags).GetValue(manager);
            filter.Conditions.Add("Factory New");
            MethodInfo getDisplayed = typeof(MarketManager).GetMethod("GetDisplayedItems", flags);

            foreach (bool buying in new[] { false, true })
            {
                typeof(MarketManager).GetField("_isBuyingTabActive", flags).SetValue(manager, buying);
                CollectionAssert.AreEqual(new[] { cheap, expensive }, (List<ItemData>)getDisplayed.Invoke(manager, null));
                manager.priceInput.text = "3";
                CollectionAssert.AreEqual(new[] { cheap }, (List<ItemData>)getDisplayed.Invoke(manager, null));
                manager.priceInput.text = "10";
            }

            manager.searchInput.text = "not found";
            Assert.That((List<ItemData>)getDisplayed.Invoke(manager, null), Is.Empty);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void UpgraderCombinesFiltersWithSortingAndUpgradeEligibility()
    {
        ItemData selected = CreateItem("Factory New", "Covert", false, "Rifle", "AK-47", 300);
        ItemData expensive = CreateItem("Factory New", "Covert", false, "Rifle", "AWP", 500);
        ItemData cheap = CreateItem("Factory New", "Covert", false, "Rifle", "AWP", 200);
        ItemData excluded = CreateItem("Minimal Wear", "Covert", false, "Rifle", "AWP", 400);
        GameObject root = new GameObject("Upgrader", typeof(RectTransform));
        try
        {
            UpgraderManager manager = root.AddComponent<UpgraderManager>();
            manager.searchInput = CreateChild<TMP_InputField>(root.transform, "Search");
            manager.priceInput = CreateChild<TMP_InputField>(root.transform, "Price");
            manager.sortDropdown = CreateChild<TMP_Dropdown>(root.transform, "Sort");
            manager.sortDropdown.options = new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("Price") };
            manager.ascendingToggle = CreateChild<Toggle>(root.transform, "Ascending");
            manager.ascendingToggle.isOn = true;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            List<ItemData> items = new List<ItemData> { expensive, excluded, cheap };
            typeof(UpgraderManager).GetField("_allItems", flags).SetValue(manager, items);
            typeof(UpgraderManager).GetField("_inventoryItems", flags).SetValue(manager, items);
            typeof(UpgraderManager).GetField("_selectedInventoryItem", flags).SetValue(manager, selected);
            ItemFilter filter = (ItemFilter)typeof(UpgraderManager).GetField("_filter", flags).GetValue(manager);
            filter.Conditions.Add("Factory New");
            MethodInfo getDisplayed = typeof(UpgraderManager).GetMethod("GetDisplayedItems", flags);

            CollectionAssert.AreEqual(new[] { cheap, expensive }, (List<ItemData>)getDisplayed.Invoke(manager, null));
            typeof(UpgraderManager).GetField("_isInventoryTabActive", flags).SetValue(manager, false);
            CollectionAssert.AreEqual(new[] { expensive }, (List<ItemData>)getDisplayed.Invoke(manager, null));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static T CreateChild<T>(Transform parent, string name) where T : Component
    {
        GameObject child = new GameObject(name, typeof(RectTransform), typeof(T));
        child.transform.SetParent(parent, false);
        return child.GetComponent<T>();
    }
}
