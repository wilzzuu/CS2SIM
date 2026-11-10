using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using Random = UnityEngine.Random;

public class RouletteManager : MonoBehaviour
{
    public static RouletteManager Instance { get; private set; }
    public GameObject rouletteItemPrefab;
    public GameObject inventoryItemPrefab;
    public GameObject itemSelectorView, gameView, pageControls;
    public Transform reelContainer;
    public Transform inventoryCatalogGrid;
    public Transform selectedItemsGrid;
    public TextMeshProUGUI selectedPlayerItemsTotalValue, chanceOfWinningText, totalValueText, totalPlayerValueText, outcomeText;
    public Button startGameButton, confirmSelectionButton, replayButton, backButton;
    public TMP_Dropdown sortDropdown;
    public Toggle ascendingToggle;

    public TextMeshProUGUI pageIndicatorText;
    private int _currentPage;
    private int _totalPages;

    const int ItemsPerPage = 28;

    private List<ItemData> _allItems = new List<ItemData>();
    private List<ItemData> _playerItems = new List<ItemData>();
    private readonly List<int> _selectedPlayerItemIndices = new List<int>();
    private readonly List<RouletteItem> _reelItems = new List<RouletteItem>();

    public int numberOfReelItems = 60;
    public float easingDuration = 7f;
    private GridLayoutGroup _gridLayout;
    private RectTransform _reelTransform;
    private int _winningItemIndex;
    private float _randomOffset;
    private bool _isScrolling;
    private Vector3 _initialReelPosition;

    private int _accumulatedBotValue;
    private int _totalPlayerValue;
    private int _totalGameValue;
    private float _winChance;
    public UIManager uiManager;

    private List<ItemData> _lastRoundPlayedItems = new List<ItemData>();
    private List<ItemData> _lastRoundWonItems = new List<ItemData>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (uiManager == null) uiManager = FindAnyObjectByType<UIManager>();

        LoadAllGameItems();
        LoadInventoryItems();

        if (sortDropdown != null)
        {
            sortDropdown.onValueChanged.AddListener(delegate { SortItems(); });
        }
        if (ascendingToggle != null)
        {
            ascendingToggle.onValueChanged.AddListener(delegate { SortItems(); });
        }

        confirmSelectionButton.onClick.AddListener(ConfirmSelection);
        startGameButton.onClick.AddListener(StartGame);
        replayButton.onClick.AddListener(AutoRematch);
        backButton.onClick.AddListener(BackToSelection);

        _gridLayout = reelContainer.GetComponent<GridLayoutGroup>();
        _reelTransform = reelContainer.GetComponent<RectTransform>();

        pageControls.SetActive(true);
        itemSelectorView.SetActive(true);
        gameView.SetActive(false);

        _currentPage = 0;
        _totalPages = Mathf.CeilToInt((float)_playerItems.Count / ItemsPerPage);
        PopulateInventoryCatalog(_currentPage);
    }

    void LoadAllGameItems()
    {
        _allItems = Resources.LoadAll<ItemData>("ItemAssets").ToList();
    }

    void LoadInventoryItems()
    {
        _playerItems = InventoryManager.Instance.GetUnlockedItems();
        _playerItems = GetSortedPlayerItems(_playerItems);
    }

    public void SortItems()
    {
        if (sortDropdown == null || sortDropdown.options.Count == 0) return;
        if (sortDropdown.options[sortDropdown.value].text == "Random")
        {
            Globals.RandomizeItemSortOrder();
        }

        List<ItemData> selectedItems = _selectedPlayerItemIndices
            .Where(index => index >= 0 && index < _playerItems.Count)
            .Select(index => _playerItems[index])
            .ToList();

        _playerItems = GetSortedPlayerItems(_playerItems);
        _selectedPlayerItemIndices.Clear();

        var usedIndices = new HashSet<int>();
        foreach (ItemData selectedItem in selectedItems)
        {
            for (int i = 0; i < _playerItems.Count; i++)
            {
                if (!usedIndices.Contains(i) && ReferenceEquals(_playerItems[i], selectedItem))
                {
                    usedIndices.Add(i);
                    _selectedPlayerItemIndices.Add(i);
                    break;
                }
            }
        }

        _currentPage = 0;
        PopulateInventoryCatalog(_currentPage);
        UpdateSelectedItemsGrid();
    }

    private List<ItemData> GetSortedPlayerItems(List<ItemData> items)
    {
        if (sortDropdown == null || sortDropdown.options.Count == 0)
        {
            return items;
        }

        string criteria = sortDropdown.options[sortDropdown.value].text;
        bool ascending = ascendingToggle == null || ascendingToggle.isOn;
        return Globals.SortItems(items, criteria, ascending);
    }

    private void PopulateInventoryCatalog(int page)
    {
        int pageCount = Mathf.CeilToInt((float)_playerItems.Count / ItemsPerPage);

        if (inventoryCatalogGrid == null) return;
        
        foreach (Transform child in inventoryCatalogGrid)
        {
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }

        int startIndex = page * ItemsPerPage;
        int endIndex = Mathf.Min(startIndex + ItemsPerPage, _playerItems.Count);
        for (int i = startIndex; i < endIndex; i++)
        {
            GameObject itemObj = Instantiate(inventoryItemPrefab, inventoryCatalogGrid);
            itemObj.GetComponent<RouletteInventoryItem>().Setup(_playerItems[i], _selectedPlayerItemIndices.Contains(i), i);
        }

        pageIndicatorText.text = $"Page {_currentPage + 1} of {(pageCount == 0 ? 1 : _totalPages)}";
    }

    public void NextPage()
    {
        int itemsCount = _playerItems.Count;
        int pageCount = Mathf.CeilToInt((float)itemsCount / ItemsPerPage);

        if (_currentPage < pageCount - 1)
        {
            _currentPage++;
            PopulateInventoryCatalog(_currentPage);
        }
    }

    public void PreviousPage()
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            PopulateInventoryCatalog(_currentPage);
        }
    }

    public void AddItemToSelection(int inventoryIndex)
    {
        if (inventoryIndex < 0 || inventoryIndex >= _playerItems.Count)
        {
            Debug.LogWarning("Cannot select an invalid inventory item.");
            return;
        }

        if (_selectedPlayerItemIndices.Contains(inventoryIndex) || _selectedPlayerItemIndices.Count >= 12)
        {
            Debug.LogWarning("Cannot select more than 12 items or select the same inventory item twice.");
            return;
        }

        ItemData item = _playerItems[inventoryIndex];
        if (item.isLocked)
        {
            Debug.LogWarning("Cannot select a locked item for roulette.");
            return;
        }

        _selectedPlayerItemIndices.Add(inventoryIndex);
        _totalPlayerValue += item.price;
        selectedPlayerItemsTotalValue.text = $"Total Value: {Globals.FormatCurrencyStr(_totalPlayerValue, Globals.EURO_DECIMAL)}€";

        UpdateSelectedItemsGrid();
    }

    public void RemoveItemFromSelection(int inventoryIndex)
    {
        if (_selectedPlayerItemIndices.Remove(inventoryIndex))
        {
            _totalPlayerValue -= _playerItems[inventoryIndex].price;
            selectedPlayerItemsTotalValue.text = $"Total Value: {Globals.FormatCurrencyStr(_totalPlayerValue, Globals.EURO_DECIMAL)}€";
        }

        UpdateSelectedItemsGrid();
    }

    private void UpdateSelectedItemsGrid()
    {
        foreach (Transform child in selectedItemsGrid)
        {
            Destroy(child.gameObject);
        }

        foreach (int inventoryIndex in _selectedPlayerItemIndices)
        {
            GameObject itemObj = Instantiate(inventoryItemPrefab, selectedItemsGrid);
            itemObj.GetComponent<RouletteInventoryItem>().Setup(_playerItems[inventoryIndex], true, inventoryIndex);
        }
    }

    void ConfirmSelection()
    {
        if (_selectedPlayerItemIndices.Count < 1)
        {
            Debug.LogWarning("Select at least one item to start.");
            return;
        }

        GenerateBotsAndPopulateReel();
        SetInitialReelPosition();
        UpdateUI();
        pageControls.SetActive(false);
        itemSelectorView.SetActive(false);
        gameView.SetActive(true);
        
        // Set button states for game ready
        startGameButton.interactable = true;
        replayButton.interactable = false;
        backButton.interactable = true;
    }

    private void SetInitialReelPosition()
    {
        float itemWidth = _gridLayout.cellSize.x + _gridLayout.spacing.x;
        float totalReelWidth = itemWidth * _reelItems.Count;

        _initialReelPosition = new Vector3(totalReelWidth / 2 - 800, reelContainer.localPosition.y, reelContainer.localPosition.z);
        reelContainer.localPosition = _initialReelPosition;
    }

    void StartGame()
    {
        if (_isScrolling || _selectedPlayerItemIndices.Count == 0 || _reelItems.Count == 0) return;

        foreach (Transform child in reelContainer)
        {
            Destroy(child.gameObject);
        }

        reelContainer.localPosition = _initialReelPosition;
        RouletteItem winningItem = GetRandomReelItem();
        _winningItemIndex = Random.Range(_reelItems.Count / 2, _reelItems.Count - 4);
        _reelItems[_winningItemIndex] = winningItem;
        
        // Disable buttons while game is running
        startGameButton.interactable = false;
        replayButton.interactable = false;
        backButton.interactable = false;
        
        StartCoroutine(AnimateScrollingReel(winningItem));
    }

    private RouletteItem GetRandomReelItem()
    {
        if (_reelItems == null || _reelItems.Count == 0)
        {
            Debug.LogWarning("Reel items are empty. Cannot get a random item.");
            return null;
        }
        
        return _reelItems[Random.Range(0, _reelItems.Count -1)];
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private IEnumerator AnimateScrollingReel(RouletteItem winningItem)
    {
        _isScrolling = true;
        uiManager.LockUI();
        startGameButton.interactable = false;
        replayButton.interactable = false;
        backButton.interactable = false;

        float itemWidth = _gridLayout.cellSize.x + _gridLayout.spacing.x;
        float totalReelWidth = itemWidth * _reelItems.Count;
        _reelTransform.sizeDelta = new Vector2(totalReelWidth, _reelTransform.sizeDelta.y);

        for (int i = 0; i < _reelItems.Count; i++)
        {
            var itemData = _reelItems[i];
            GameObject reelItem = Instantiate(rouletteItemPrefab, reelContainer);
            SetUpReelItem(reelItem, itemData.Item, itemData.OwnerIsPlayer);
        }

        _randomOffset = Random.Range(0f, 256f);
        float targetPosition = _initialReelPosition.x - (itemWidth * _winningItemIndex) + 800 - _randomOffset;
        float elapsed = 0f;

        while (elapsed < easingDuration)
        {
            float t = elapsed / easingDuration;
            float erateFactor = ErateOutQuint(t);

            float currentPosition = Mathf.Lerp(_initialReelPosition.x, targetPosition, erateFactor);
            reelContainer.localPosition = new Vector3(currentPosition, reelContainer.localPosition.y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        reelContainer.localPosition = new Vector3(targetPosition, reelContainer.localPosition.y, 0);
        
        // Settle outcome only after animation completes
        SettleOutcome(winningItem);
        
        outcomeText.text = winningItem.OwnerIsPlayer ? "You won!" : "You lost!";
        _isScrolling = false;
        uiManager.UnlockUI();
        startGameButton.interactable = false;
        replayButton.interactable = true;
        backButton.interactable = false;
    }

    private void SettleOutcome(RouletteItem winningItem)
    {
        // Record played items
        _lastRoundPlayedItems.Clear();
        foreach (int inventoryIndex in _selectedPlayerItemIndices)
        {
            _lastRoundPlayedItems.Add(_playerItems[inventoryIndex]);
        }

        _lastRoundWonItems.Clear();

        if (winningItem.OwnerIsPlayer)
        {
            HashSet<ItemData> uniqueBotItemsToAdd = new HashSet<ItemData>();

            foreach (var item in _reelItems)
            {
                if (!item.OwnerIsPlayer)
                {
                    uniqueBotItemsToAdd.Add(item.Item);
                    _lastRoundWonItems.Add(item.Item);
                }
            }

            foreach (var uniqueBotItem in uniqueBotItemsToAdd)
            {
                InventoryManager.Instance.AddItemToInventory(uniqueBotItem);
                CollectionManager.Instance.AddItemToCollection(uniqueBotItem);
            }
        }
        else
        {
            foreach (int inventoryIndex in _selectedPlayerItemIndices)
            {
                InventoryManager.Instance.RemoveItemFromInventory(_playerItems[inventoryIndex]);
            }
        }

        // Record the round outcome for purchase system
        this.RecordRoundOutcome(_lastRoundPlayedItems, _lastRoundWonItems);
    }

    private void SetUpReelItem(GameObject reelItem, ItemData itemData, bool isPlayerOwned)
    {
        Image itemImage = reelItem.transform.Find("ItemImage").GetComponent<Image>();
        Image ownerImage = reelItem.transform.Find("OwnerImage").GetComponent<Image>();

        itemImage.sprite = Resources.Load<Sprite>($"ItemImages/{itemData.imageName}");
        ownerImage.sprite = Resources.Load<Sprite>($"Images/General/{(isPlayerOwned ? "Player" : "House")}");
    }

    private float ErateOutQuint(float t)
    {
        return 1 - Mathf.Pow(1 - t, 5);
    }

    void UpdateUI()
    {
        int totalPlayerItemValue = _selectedPlayerItemIndices.Sum(index => _playerItems[index].price);
        int totalUniqueBotValue = _accumulatedBotValue;

        _totalGameValue = totalPlayerItemValue + totalUniqueBotValue;

        int playerItemCount = _selectedPlayerItemIndices.Count;
        int reelItemCount = _reelItems.Count;

        _winChance = reelItemCount > 0 ? (float)playerItemCount / reelItemCount * 100f : 0f;

        chanceOfWinningText.text = $"Win Chance: {_winChance:F2}%";
        totalPlayerValueText.text = $"Total Player Value: {Globals.FormatCurrencyStr(totalPlayerItemValue, Globals.EURO_DECIMAL)}€";
        totalValueText.text = $"Total Game Value: {Globals.FormatCurrencyStr(_totalGameValue, Globals.EURO_DECIMAL)}€";
        outcomeText.text = "";
    }

    void GenerateBotsAndPopulateReel()
    {
        _reelItems.Clear();
        _accumulatedBotValue = 0;

        foreach (int inventoryIndex in _selectedPlayerItemIndices)
        {
            ItemData playerItem = _playerItems[inventoryIndex];
            var playerRouletteItem = new RouletteItem { Item = playerItem, OwnerIsPlayer = true };
            _reelItems.Add(playerRouletteItem);
        }

        int playerItemCount = _selectedPlayerItemIndices.Count;
        int botItemsCount = Mathf.Clamp(playerItemCount * 5, 1, numberOfReelItems - playerItemCount);

        float scalingFactor = (float)numberOfReelItems / playerItemCount;
        float botItemTargetValue = _totalPlayerValue * scalingFactor;
        HashSet<ItemData> usedBotItems = new HashSet<ItemData>();

        for (int i = 0; i < botItemsCount; i++)
        {
            var botItem = GenerateRandomBotItem(botItemTargetValue / botItemsCount, usedBotItems);
            var botRouletteItem = new RouletteItem { Item = botItem, OwnerIsPlayer = false };
            _reelItems.Add(botRouletteItem);
            _accumulatedBotValue += botItem.price;
        }

        int targetReelSize = Mathf.Max(numberOfReelItems, 40);
        for (int i = 0; _reelItems.Count < targetReelSize; i++)
        {
            var botItemToAdd = _reelItems[(i % botItemsCount) + playerItemCount];
            _reelItems.Add(new RouletteItem { Item = botItemToAdd.Item, OwnerIsPlayer = false });
        }

        ShuffleReelItems();
        UpdateUI();
    }
        
    void ShuffleReelItems()
    {
        for (int i = 0; i < _reelItems.Count; i++)
        {
            int randomIndex = Random.Range(i, _reelItems.Count);
            RouletteItem temp = _reelItems[i];
            _reelItems[i] = _reelItems[randomIndex];
            _reelItems[randomIndex] = temp;
        }
    }

    private ItemData GenerateRandomBotItem(float targetValue, HashSet<ItemData> usedBotItems)
    {
        if (_allItems == null || _allItems.Count == 0)
        {
            _allItems = Resources.LoadAll<ItemData>("ItemAssets").ToList();
        }

        float maxVariance = 5.0f;
        float priceVariance = Mathf.Min(targetValue * 0.3f, maxVariance);

        List<ItemData> eligibleItems = _allItems
            .Where(item => item.price >= targetValue - priceVariance && item.price <= targetValue + priceVariance)
            .Where(item => !usedBotItems.Contains(item))
            .ToList();

        if (eligibleItems.Count < 5)
        {
            eligibleItems = _allItems
                .Where(item => !usedBotItems.Contains(item))
                .OrderBy(item => Mathf.Abs(item.price - targetValue))
                .Take(15)
                .ToList();
        }

        ItemData selectedItem = eligibleItems[Random.Range(0, eligibleItems.Count)];
        usedBotItems.Add(selectedItem);
        return selectedItem;
    }

    void RestartGame()
    {
        _selectedPlayerItemIndices.Clear();
        _reelItems.Clear();
        LoadInventoryItems();

        foreach (Transform child in reelContainer)
        {
            Destroy(child.gameObject);
        }

        _totalPlayerValue = 0;
        _totalGameValue = 0;
        _winChance = 0f;
        startGameButton.interactable = true;
        backButton.interactable = true;
        pageControls.SetActive(true);
        itemSelectorView.SetActive(true);
        UpdateSelectedItemsGrid();
        PopulateInventoryCatalog(0);
        selectedPlayerItemsTotalValue.text = "Total Value: 0€";
        gameView.SetActive(false);
        
        // Clear purchase history when restarting
        this.ClearPurchaseHistory();
    }

    /// <summary>
    /// Automated rematch function that handles the replay workflow:
    /// 1. Auto-purchases lost items from previous round
    /// 2. Auto-selects the purchased items into the roulette game
    /// 3. Returns to game view ready for player to click Start Game
    /// </summary>
    public void AutoRematch()
    {
        // Check if we have items from the last round to replay
        List<ItemData> lastPlayedItems = this.GetLastPlayedItems();
        if (lastPlayedItems == null || lastPlayedItems.Count == 0)
        {
            Debug.LogWarning("No items from previous round to replay.");
            return;
        }

        // Check if player lost the last game (if they won, they don't need to repurchase)
        List<ItemData> winningItems = this.GetLastWonItems();
        bool playerLostLastGame = (winningItems == null || winningItems.Count == 0);

        if (playerLostLastGame)
        {
            // Auto-purchase the items that were lost
            if (!this.TryPurchaseItemsForReplay(lastPlayedItems))
            {
                Debug.LogWarning("Failed to auto-purchase items for rematch. Insufficient balance.");
                return;
            }
        }

        // Reload inventory after purchase
        LoadInventoryItems();

        // Reset game state
        _isScrolling = false;
        _selectedPlayerItemIndices.Clear();
        _totalPlayerValue = 0;
        _reelItems.Clear();

        // Auto-select the last played items
        for (int i = 0; i < _playerItems.Count; i++)
        {
            foreach (var lastPlayedItem in lastPlayedItems)
            {
                if (ReferenceEquals(_playerItems[i], lastPlayedItem) && 
                    !_selectedPlayerItemIndices.Contains(i) &&
                    _selectedPlayerItemIndices.Count < 12)
                {
                    _selectedPlayerItemIndices.Add(i);
                    _totalPlayerValue += _playerItems[i].price;
                    break;
                }
            }
        }

        // If we successfully selected items, prepare game but let player click Start Game
        if (_selectedPlayerItemIndices.Count > 0)
        {
            // Clear previous game's visual elements
            foreach (Transform child in reelContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Reset reel position
            reelContainer.localPosition = _initialReelPosition;
            
            // Generate new bot items and populate reel
            GenerateBotsAndPopulateReel();
            SetInitialReelPosition();
            UpdateUI();
            
            // Clear outcome text from previous game
            outcomeText.text = "";
            
            // Update the UI display
            UpdateSelectedItemsGrid();
            selectedPlayerItemsTotalValue.text = $"Total Value: {Globals.FormatCurrencyStr(_totalPlayerValue, Globals.EURO_DECIMAL)}€";
            
            // Update UI visibility
            pageControls.SetActive(false);
            itemSelectorView.SetActive(false);
            gameView.SetActive(true);
            
            // Set button states for replay preparation
            startGameButton.interactable = true;
            replayButton.interactable = false;
            backButton.interactable = true;
        }
    }

    void BackToSelection()
    {
        RestartGame();
    }

    /// <summary>
    /// Purchases a specific item won from the last roulette round.
    /// Requires sufficient player balance.
    /// </summary>
    public bool PurchaseWonItem(ItemData item)
    {
        if (item == null)
        {
            Debug.LogWarning("Cannot purchase a null item.");
            return false;
        }

        if (!_lastRoundWonItems.Contains(item))
        {
            Debug.LogWarning("Item was not won in the last round and cannot be purchased.");
            return false;
        }

        return this.TryPurchaseItemForReplay(item);
    }

    /// <summary>
    /// Purchases multiple items won from the last roulette round.
    /// All items must be affordable or the purchase is cancelled.
    /// </summary>
    public bool PurchaseWonItems(List<ItemData> items)
    {
        if (items == null || items.Count == 0)
        {
            Debug.LogWarning("Cannot purchase an empty item list.");
            return false;
        }

        foreach (var item in items)
        {
            if (!_lastRoundWonItems.Contains(item))
            {
                Debug.LogWarning($"Item '{item.name}' was not won in the last round.");
                return false;
            }
        }

        return this.TryPurchaseItemsForReplay(items);
    }

    /// <summary>
    /// Gets the purchase manager for the last round's won items.
    /// Use this to manage batch purchases programmatically.
    /// </summary>
    public RoulettePurchaseManager GetPurchaseManager()
    {
        return this.GetPurchaseManager();
    }


    /// <summary>
    /// Gets all items the player used in the last roulette round.
    /// </summary>
    public List<ItemData> GetLastPlayedItems()
    {
        return new List<ItemData>(_lastRoundPlayedItems);
    }

    /// <summary>
    /// Gets all items the player won in the last roulette round.
    /// </summary>
    public List<ItemData> GetLastWonItems()
    {
        return new List<ItemData>(_lastRoundWonItems);
    }

    /// <summary>
    /// Checks if the player can afford to purchase a specific item.
    /// </summary>
    public bool CanAffordItem(ItemData item)
    {
        return this.CanAffordItem(item);
    }

    /// <summary>
    /// Gets the total cost to purchase a list of items.
    /// </summary>
    public int GetPurchaseCost(List<ItemData> items)
    {
        return this.GetPurchaseCost(items);
    }
}
