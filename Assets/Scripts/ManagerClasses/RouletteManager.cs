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

    public TextMeshProUGUI currentPageText;
    public TextMeshProUGUI maxPageText;
    private int currentPage;

    const int ItemsPerPage = 28;

    private List<ItemData> _allItems = new List<ItemData>();
    private List<ItemData> _playerItems = new List<ItemData>();
    private readonly List<ItemData> _selectedPlayerItems = new List<ItemData>();
    private readonly List<RouletteItem> _reelItems = new List<RouletteItem>();
    private readonly Dictionary<ItemData, bool> _itemOwnership = new Dictionary<ItemData, bool>();

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

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        LoadAllGameItems();
        LoadInventoryItems();

        confirmSelectionButton.onClick.AddListener(ConfirmSelection);
        startGameButton.onClick.AddListener(StartGame);
        replayButton.onClick.AddListener(RestartGame);
        backButton.onClick.AddListener(BackToSelection);

        _gridLayout = reelContainer.GetComponent<GridLayoutGroup>();
        _reelTransform = reelContainer.GetComponent<RectTransform>();

        pageControls.SetActive(true);
        itemSelectorView.SetActive(true);
        gameView.SetActive(false);

        currentPage = 0;
        PopulateInventoryCatalog(currentPage);
    }

    void LoadAllGameItems()
    {
        _allItems = Resources.LoadAll<ItemData>("ItemAssets").ToList();
    }

    void LoadInventoryItems()
    {
        _playerItems = InventoryManager.Instance.GetAllItems();
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
            itemObj.GetComponent<RouletteInventoryItem>().Setup(_playerItems[i], _selectedPlayerItems.Contains(_playerItems[i]));
        }

        currentPageText.text = ((pageCount > 0) ? page+1 : page).ToString();
        maxPageText.text = pageCount.ToString();
    }

    public void NextPage()
    {
        int itemsCount = _playerItems.Count;
        int pageCount = Mathf.CeilToInt((float)itemsCount / ItemsPerPage);

        if (currentPage < pageCount - 1)
        {
            currentPage++;
            PopulateInventoryCatalog(currentPage);
        }
    }

    public void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            PopulateInventoryCatalog(currentPage);
        }
    }

    public void AddItemToSelection(ItemData item)
    {
        if (_selectedPlayerItems.Contains(item) || _selectedPlayerItems.Count >= 12)
        {
            Debug.LogWarning("Cannot select duplicates or more than 12 items.");
            return;
        }

        _selectedPlayerItems.Add(item);
        _totalPlayerValue += item.price;
        selectedPlayerItemsTotalValue.text = $"Total Value: {Globals.FormatCurrencyStr(_totalPlayerValue, Globals.EURO_DECIMAL)}€";

        UpdateSelectedItemsGrid();
    }

    public void RemoveItemFromSelection(ItemData item)
    {
        if (_selectedPlayerItems.Remove(item))
        {
            _totalPlayerValue -= item.price;
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

        foreach (var item in _selectedPlayerItems)
        {
            GameObject itemObj = Instantiate(inventoryItemPrefab, selectedItemsGrid);
            itemObj.GetComponent<RouletteInventoryItem>().Setup(item, true);
        }
    }

    void ConfirmSelection()
    {
        if (_selectedPlayerItems.Count < 1)
        {
            Debug.LogWarning("Select at least one item to start.");
            return;
        }

        GenerateBotsAndPopulateReel();
        SetInitialReelPosition();
        UpdateUI();
        pageControls.SetActive(false);
        itemSelectorView.SetActive(false);
        replayButton.interactable = false;
        gameView.SetActive(true);
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
        if (_isScrolling || _selectedPlayerItems.Count == 0 || _reelItems.Count == 0) return;

        foreach (Transform child in reelContainer)
        {
            Destroy(child.gameObject);
        }

        reelContainer.localPosition = _initialReelPosition;
        RouletteItem winningItem = GetRandomReelItem();
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

        _winningItemIndex = Random.Range(_reelItems.Count / 2, _reelItems.Count - 4);
        _reelItems[_winningItemIndex] = winningItem;

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
        EvaluateOutcome(winningItem);
        _isScrolling = false;
        uiManager.UnlockUI();
        startGameButton.interactable = false;
        replayButton.interactable = true;
        backButton.interactable = false;
    }

    private void EvaluateOutcome(RouletteItem winningItem)
    {
        if (winningItem.OwnerIsPlayer)
        {
            HashSet<ItemData> uniqueBotItemsToAdd = new HashSet<ItemData>();

            foreach (var item in _reelItems)
            {
                if (!_itemOwnership[item.Item])
                {
                    uniqueBotItemsToAdd.Add(item.Item);
                }
            }

            foreach (var uniqueBotItem in uniqueBotItemsToAdd)
            {
                InventoryManager.Instance.AddItemToInventory(uniqueBotItem);
                CollectionManager.Instance.AddItemToCollection(uniqueBotItem);
            }
            outcomeText.text = "You won!";
        }
        else
        {
            foreach (var item in _selectedPlayerItems)
            {
                InventoryManager.Instance.RemoveItemFromInventory(item);
            }
            outcomeText.text = "You lost!";
        }
        replayButton.interactable = true;
    }

    private void SetUpReelItem(GameObject reelItem, ItemData itemData, bool isPlayerOwned)
    {
        Image itemImage = reelItem.transform.Find("ItemImage").GetComponent<Image>();
        Image rarityImage = reelItem.transform.Find("RarityImage").GetComponent<Image>();
        TextMeshProUGUI ownerText = reelItem.transform.Find("OwnerText").GetComponent<TextMeshProUGUI>();

        itemImage.sprite = Resources.Load<Sprite>($"ItemImages/{itemData.imageName}");
        rarityImage.sprite = Resources.Load<Sprite>($"RarityImages/{itemData.rarity}");

        ownerText.text = isPlayerOwned ? "Player" : "Bot";
        ownerText.color = isPlayerOwned ? Color.green : Color.red;
    }

    private float ErateOutQuint(float t)
    {
        return 1 - Mathf.Pow(1 - t, 5);
    }

    void UpdateUI()
    {
        int totalPlayerItemValue = _selectedPlayerItems.Sum(item => item.price);
        int totalUniqueBotValue = _accumulatedBotValue;

        _totalGameValue = totalPlayerItemValue + totalUniqueBotValue;

        int playerItemCount = _selectedPlayerItems.Count;
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
        _itemOwnership.Clear();
        _accumulatedBotValue = 0;

        foreach (var playerItem in _selectedPlayerItems)
        {
            var playerRouletteItem = new RouletteItem { Item = playerItem, OwnerIsPlayer = true };
            _reelItems.Add(playerRouletteItem);
            _itemOwnership[playerItem] = true;
        }

        int playerItemCount = _selectedPlayerItems.Count;
        int botItemsCount = Mathf.Clamp(playerItemCount * 5, 1, numberOfReelItems - playerItemCount);

        float scalingFactor = (float)numberOfReelItems / playerItemCount;
        float botItemTargetValue = _totalPlayerValue * scalingFactor;
        HashSet<ItemData> usedBotItems = new HashSet<ItemData>();

        for (int i = 0; i < botItemsCount; i++)
        {
            var botItem = GenerateRandomBotItem(botItemTargetValue / botItemsCount, usedBotItems);
            var botRouletteItem = new RouletteItem { Item = botItem, OwnerIsPlayer = false };
            _reelItems.Add(botRouletteItem);
            _itemOwnership[botItem] = false;
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
        LoadInventoryItems();
        _selectedPlayerItems.Clear();
        _reelItems.Clear();
        _itemOwnership.Clear();

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
    }

    void BackToSelection()
    {
        RestartGame();
    }
}
