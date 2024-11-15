using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using UnityEngine.Rendering.UI;
using Random = UnityEngine.Random;

public class RouletteManager : MonoBehaviour
{
    public static RouletteManager Instance { get; private set; }
    public GameObject rouletteItemPrefab;
    public GameObject inventoryItemPrefab;
    public GameObject itemSelectorView, gameView;
    public Transform reelContainer;
    public Transform inventoryCatalogGrid;
    public Transform selectedItemsGrid;
    public TextMeshProUGUI selectedPlayerItemsTotalValue, chanceOfWinningText, totalValueText, totalPlayerValueText, outcomeText;
    public Button startGameButton, confirmSelectionButton, replayButton, backButton;

    private List<ItemData> _allItems = new List<ItemData>();
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

    private float _accumulatedBotValue;
    private float _totalPlayerValue;
    private float _totalGameValue;
    private float _winChance;
    public UIManager uiManager;
    
    private static readonly Dictionary<string, float> RarityWeights = new Dictionary<string, float>
    {
        {"MIL_SPEC", 0.7992f},
        {"MIL_SPEC StatTrak", 0.07992f},
        {"RESTRICTED", 0.1598f},
        {"RESTRICTED StatTrak", 0.01598f},
        {"CLASSIFIED", 0.032f},
        {"CLASSIFIED StatTrak", 0.0032f},
        {"COVERT", 0.0064f},
        {"COVERT StatTrak", 0.00064f},
        {"SPECIAL", 0.0026f},
        {"SPECIAL StatTrak", 0.00026f}
    };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        confirmSelectionButton.onClick.AddListener(ConfirmSelection);
        startGameButton.onClick.AddListener(StartGame);
        replayButton.onClick.AddListener(RestartGame);
        backButton.onClick.AddListener(BackToSelection);

        _gridLayout = reelContainer.GetComponent<GridLayoutGroup>();
        _reelTransform = reelContainer.GetComponent<RectTransform>();

        itemSelectorView.SetActive(true);
        gameView.SetActive(false);

        PopulateInventoryCatalog();
    }

    private void PopulateInventoryCatalog()
    {
        foreach (Transform child in inventoryCatalogGrid)
        {
            Destroy(child.gameObject);
        }

        foreach (var item in InventoryManager.Instance.GetAllItems())
        {
            GameObject itemObj = Instantiate(inventoryItemPrefab, inventoryCatalogGrid);
            itemObj.GetComponent<RouletteInventoryItem>().Setup(item, _selectedPlayerItems.Contains(item));
        }
    }

    public void AddItemToSelection(ItemData item)
    {
        if (_selectedPlayerItems.Contains(item) || _selectedPlayerItems.Count >= 12)
        {
            Debug.LogWarning("Cannot select more than 10 items or duplicate items.");
            return;
        }

        _selectedPlayerItems.Add(item);
        _totalPlayerValue += item.price;
        selectedPlayerItemsTotalValue.text = $"Total Value: {_totalPlayerValue:F2}€";

        UpdateSelectedItemsGrid();
    }

    public void RemoveItemFromSelection(ItemData item)
    {
        if (_selectedPlayerItems.Remove(item))
        {
            _totalPlayerValue -= item.price;
            selectedPlayerItemsTotalValue.text = $"Total Value: {_totalPlayerValue:F2}€";
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
        itemSelectorView.SetActive(false);
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
        RouletteItem winningItem = GetRandomItemByChance();
        StartCoroutine(AnimateScrollingReel(winningItem));
    }

    private RouletteItem GetRandomItemByChance()
    {
        if (_reelItems == null || _reelItems.Count == 0)
        {
            Debug.LogWarning("Reel items are empty. Cannot get a random item.");
            return null;
        }

        float totalWeight = _reelItems
            .Where(i => RarityWeights.ContainsKey(i.Item.rarity))
            .Sum(i => i.OwnerIsPlayer ? RarityWeights[i.Item.rarity] : RarityWeights[i.Item.rarity] / 10);

        if (totalWeight <= 0)
        {
            Debug.LogWarning("Total weight is zero or invalid. Cannot get a random item.");
            return null;
        }

        float randomValue = Random.Range(0, totalWeight);
        float cumulativeWeight = 0f;

        foreach (var item in _reelItems)
        {
            float itemWeight = item.OwnerIsPlayer
                ? RarityWeights[item.Item.rarity]
                : RarityWeights[item.Item.rarity] / 10;

            cumulativeWeight += itemWeight;
            if (randomValue <= cumulativeWeight)
            {
                return item;
            }
        }

        return _reelItems[_reelItems.Count - 1];
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
    }

    private void SetUpReelItem(GameObject reelItem, ItemData itemData, bool isPlayerOwned)
    {
        Image itemImage = reelItem.transform.Find("ItemImage").GetComponent<Image>();
        Image rarityImage = reelItem.transform.Find("RarityImage").GetComponent<Image>();
        TextMeshProUGUI ownerText = reelItem.transform.Find("OwnerText").GetComponent<TextMeshProUGUI>();

        itemImage.sprite = Resources.Load<Sprite>($"ItemImages/{itemData.id}");
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
        float totalPlayerItemValue = _selectedPlayerItems.Sum(item => item.price);
        float totalUniqueBotValue = _accumulatedBotValue;

        _totalGameValue = totalPlayerItemValue + totalUniqueBotValue;

        int playerItemCount = _selectedPlayerItems.Count;
        int reelItemCount = _reelItems.Count;

        _winChance = reelItemCount > 0 ? (float)playerItemCount / reelItemCount * 100f : 0f;

        chanceOfWinningText.text = $"Win Chance: {_winChance:F2}%";
        totalPlayerValueText.text = $"Total Player Value: {totalPlayerItemValue:F2}€";
        totalValueText.text = $"Total Game Value: {_totalGameValue:F2}€";
        outcomeText.text = "";
    }

    void GenerateBotsAndPopulateReel()
    {
        _reelItems.Clear();
        _itemOwnership.Clear();
        _accumulatedBotValue = 0f;
        
        Debug.Log($"Generating reel with {numberOfReelItems} items. Player selected: {_selectedPlayerItems.Count}");

        // Add player items to the reel
        foreach (var playerItem in _selectedPlayerItems)
        {
            Debug.Log($"Adding player item: {playerItem.name} (Value: {playerItem.price})");
            var playerRouletteItem = new RouletteItem { Item = playerItem, OwnerIsPlayer = true };
            _reelItems.Add(playerRouletteItem);
            _itemOwnership[playerItem] = true;
        }

        int playerItemCount = _selectedPlayerItems.Count;
        int botItemsCount = Mathf.Clamp(numberOfReelItems - playerItemCount, 1, numberOfReelItems);

        HashSet<ItemData> usedBotItems = new HashSet<ItemData>();

        // Generate bot items to balance the reel
        for (int i = 0; i < botItemsCount; i++)
        {
            var botItem = GenerateRandomBotItem(_totalPlayerValue / botItemsCount, usedBotItems);
            Debug.Log($"Adding bot item: {botItem.name} (Value: {botItem.price})");
            var botRouletteItem = new RouletteItem { Item = botItem, OwnerIsPlayer = false };
            _reelItems.Add(botRouletteItem);
            _itemOwnership[botItem] = false;
            _accumulatedBotValue += botItem.price;
        }

        // Ensure the reel has exactly `numberOfReelItems` items
        while (_reelItems.Count < numberOfReelItems)
        {
            var botItemToAdd = _reelItems[Random.Range(playerItemCount, _reelItems.Count)];
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
            (_reelItems[i], _reelItems[randomIndex]) = (_reelItems[randomIndex], _reelItems[i]);
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

        ItemData selectedItem = eligibleItems[UnityEngine.Random.Range(0, eligibleItems.Count)];
        usedBotItems.Add(selectedItem);
        return selectedItem;
    }

    void RestartGame()
    {
        _selectedPlayerItems.Clear();
        _reelItems.Clear();
        _itemOwnership.Clear();

        foreach (Transform child in reelContainer)
        {
            Destroy(child.gameObject);
        }

        _totalPlayerValue = 0f;
        _totalGameValue = 0f;
        _winChance = 0f;
        startGameButton.interactable = true;
        backButton.interactable = true;
        itemSelectorView.SetActive(true);
        UpdateSelectedItemsGrid();
        PopulateInventoryCatalog();
        selectedPlayerItemsTotalValue.text = "Total Value: 0";
        gameView.SetActive(false);
    }

    void BackToSelection()
    {
        RestartGame();
    }
}
