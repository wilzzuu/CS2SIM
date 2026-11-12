using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class CollectionData
{
    public List<string> collectedItemIDs;
}

public sealed class CollectionStore
{
    private readonly string _path;
    private readonly HashSet<string> _ids;

    public CollectionStore(string path)
    {
        _path = path;
        if (!File.Exists(path))
        {
            _ids = new HashSet<string>();
            return;
        }

        string json = DataEncryptionUtility.Decrypt(File.ReadAllText(path));
        CollectionData data = JsonUtility.FromJson<CollectionData>(json);
        if (data == null || data.collectedItemIDs == null)
            throw new InvalidDataException($"Invalid collection data in '{path}'.");
        _ids = new HashSet<string>(data.collectedItemIDs);
    }

    public bool Contains(string id) => _ids.Contains(id);

    public bool Add(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Collection item ID must not be empty.", nameof(id));
        if (_ids.Contains(id)) return false;

        HashSet<string> updated = new HashSet<string>(_ids) { id };
        Save(updated);
        _ids.Add(id);
        return true;
    }

    public void Clear()
    {
        Save(new HashSet<string>());
        _ids.Clear();
    }

    private void Save(HashSet<string> ids)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path));
        CollectionData data = new CollectionData { collectedItemIDs = new List<string>(ids) };
        File.WriteAllText(_path, DataEncryptionUtility.Encrypt(JsonUtility.ToJson(data)));
    }
}

public class CollectionManager : MonoBehaviour, IItemFilterScreen
{
    public static CollectionManager Instance { get; private set; }
    private static CollectionStore _store;
    private static ItemData[] _itemCatalog;

    public GameObject collectionItemPrefab;
    public Transform collectionGrid;
    public TMP_Text collectionPercentageText;
    public TMP_Text pageIndicatorText;
    public TMP_Dropdown sortDropdown;
    public Toggle ascendingToggle;
    public TMP_InputField searchInput;
    [Tooltip("Options in order: All, Collected, Not Collected.")]
    public TMP_Dropdown collectionStatusDropdown;
    public Button previousPageButton;
    public Button nextPageButton;
    [Min(1)] public int itemsPerPage = 40;

    private readonly ItemFilter _filter = new ItemFilter();
    private List<ItemData> _allItems = new List<ItemData>();
    private List<ItemData> _visibleItems = new List<ItemData>();
    private int _currentPage;
    private bool _initialized;

    private static CollectionStore Store
    {
        get
        {
            if (_store == null)
            {
                #if UNITY_EDITOR
                string directory = "EditorData";
                #else
                string directory = "SaveData";
                #endif
                _store = new CollectionStore(Path.Combine(Application.persistentDataPath, directory, "CollectionData.save"));
            }
            return _store;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        _store = null;
        _itemCatalog = null;
        Instance = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Multiple CollectionManager instances exist in the same scene.", this);
            enabled = false;
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (collectionGrid == null) return;
        CollectionItem itemTemplate = collectionItemPrefab == null ? null : collectionItemPrefab.GetComponent<CollectionItem>();
        if (itemTemplate == null || itemTemplate.itemImage == null || itemTemplate.rarityImage == null ||
            itemTemplate.gunText == null || itemTemplate.nameText == null || itemTemplate.conditionText == null || itemTemplate.statTrakText == null || itemTemplate.grayscaleMaterial == null)
        {
            Debug.LogError("Collection requires a CollectionItem prefab with image, rarity, gun, name, condition, statTrak text and grayscale material references.", this);
            return;
        }
        if (itemsPerPage < 1)
        {
            Debug.LogError("Collection items per page must be greater than zero.", this);
            return;
        }

        if (_itemCatalog == null)
            _itemCatalog = Resources.LoadAll<ItemData>("ItemAssets").Where(item => item != null).ToArray();
        _allItems = _itemCatalog.ToList();
        if (sortDropdown != null) sortDropdown.onValueChanged.AddListener(OnSortChanged);
        if (ascendingToggle != null) ascendingToggle.onValueChanged.AddListener(OnSortOrderChanged);
        if (searchInput != null) searchInput.onValueChanged.AddListener(OnSearchChanged);
        if (collectionStatusDropdown != null) collectionStatusDropdown.onValueChanged.AddListener(OnStatusChanged);
        if (previousPageButton != null) previousPageButton.onClick.AddListener(PreviousPage);
        if (nextPageButton != null) nextPageButton.onClick.AddListener(NextPage);
        _initialized = true;
        RefreshItems();
    }

    private void OnDestroy()
    {
        if (sortDropdown != null) sortDropdown.onValueChanged.RemoveListener(OnSortChanged);
        if (ascendingToggle != null) ascendingToggle.onValueChanged.RemoveListener(OnSortOrderChanged);
        if (searchInput != null) searchInput.onValueChanged.RemoveListener(OnSearchChanged);
        if (collectionStatusDropdown != null) collectionStatusDropdown.onValueChanged.RemoveListener(OnStatusChanged);
        if (previousPageButton != null) previousPageButton.onClick.RemoveListener(PreviousPage);
        if (nextPageButton != null) nextPageButton.onClick.RemoveListener(NextPage);
        if (Instance == this) Instance = null;
    }

    public static void RecordItem(ItemData item)
    {
        if (item == null)
        {
            Debug.LogError("Cannot add a null item to the collection.");
            return;
        }

        if (Store.Add(item.id) && Instance != null && Instance._initialized)
            Instance.RefreshItems();
    }

    public static bool IsCollected(string id) => Store.Contains(id);

    public static void ResetCollection()
    {
        Store.Clear();
        if (Instance != null && Instance._initialized) Instance.RefreshItems();
    }

    public void AddItemToCollection(ItemData item) => RecordItem(item);
    public void ClearCollection() => ResetCollection();
    public void UpdateCollectionUI() => RefreshItems();

    public void OpenFilters()
    {
        if (!_initialized)
        {
            Debug.LogError("Cannot open collection filters before the collection is initialized.", this);
            return;
        }
        ItemFilterPopup.Open(collectionGrid, _filter, _allItems, RefreshItems);
    }

    private void OnSortChanged(int _) => SortItems();
    private void OnSortOrderChanged(bool _) => SortItems();
    private void OnSearchChanged(string _) => RefreshItems();
    private void OnStatusChanged(int _) => RefreshItems();

    public void SortItems()
    {
        if (sortDropdown != null && sortDropdown.value >= 0 && sortDropdown.value < sortDropdown.options.Count &&
            sortDropdown.options[sortDropdown.value].text == "Random")
            Globals.RandomizeItemSortOrder();
        RefreshItems();
    }

    private void RefreshItems()
    {
        if (!_initialized) return;
        IEnumerable<ItemData> items = _filter.Apply(_allItems);
        string query = searchInput == null ? string.Empty : searchInput.text.Trim();
        if (!string.IsNullOrEmpty(query))
            items = items.Where(item =>
                (item.name != null && item.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (item.id != null && item.id.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (item.gun != null && item.gun.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0));

        if (collectionStatusDropdown != null)
        {
            if (collectionStatusDropdown.value == 1) items = items.Where(item => IsCollected(item.id));
            else if (collectionStatusDropdown.value == 2) items = items.Where(item => !IsCollected(item.id));
        }

        if (sortDropdown != null && sortDropdown.value >= 0 && sortDropdown.value < sortDropdown.options.Count)
            _visibleItems = Globals.SortItems(items, sortDropdown.options[sortDropdown.value].text,
                ascendingToggle == null || ascendingToggle.isOn);
        else
            _visibleItems = items.ToList();
        _currentPage = 0;
        DisplayPage();
    }

    private void DisplayPage()
    {
        foreach (Transform child in collectionGrid)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        int pageCount = Mathf.Max(1, Mathf.CeilToInt((float)_visibleItems.Count / itemsPerPage));
        _currentPage = Mathf.Clamp(_currentPage, 0, pageCount - 1);
        if (pageIndicatorText != null) pageIndicatorText.text = $"Page {_currentPage + 1} of {pageCount}";
        if (previousPageButton != null) previousPageButton.interactable = _currentPage > 0;
        if (nextPageButton != null) nextPageButton.interactable = _currentPage < pageCount - 1;
        if (collectionPercentageText != null)
        {
            float collectedCount = _visibleItems.Count(item => IsCollected(item.id));
            float totalCount = _visibleItems.Count;
            collectionPercentageText.text = $"{collectedCount}/{totalCount}  ({(totalCount > 0 ? (collectedCount / totalCount * 100f) : 0f):F2}%)";
        }

        int start = _currentPage * itemsPerPage;
        int end = Mathf.Min(start + itemsPerPage, _visibleItems.Count);
        for (int i = start; i < end; i++)
        {
            ItemData item = _visibleItems[i];
            Instantiate(collectionItemPrefab, collectionGrid).GetComponent<CollectionItem>()
                .Setup(item, IsCollected(item.id), Resources.Load<Sprite>($"ItemImages/{item.imageName}"), Resources.Load<Sprite>($"RarityImages/{item.rarity}_COLLECTION"));
        }
    }

    public void NextPage()
    {
        if (!_initialized || (_currentPage + 1) * itemsPerPage >= _visibleItems.Count) return;
        _currentPage++;
        DisplayPage();
    }

    public void PreviousPage()
    {
        if (!_initialized || _currentPage == 0) return;
        _currentPage--;
        DisplayPage();
    }
}
