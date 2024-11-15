using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CaseBattleManager : MonoBehaviour
{
    public Transform playerReelParent;
    public Transform botReelParent;
    public GameObject reelItemPrefab;
    public GameObject resultPrefab;
    public Transform playerResultArea;
    public Transform botResultArea;
    public TextMeshProUGUI playerTotalValueText;
    public TextMeshProUGUI botTotalValueText;
    public TextMeshProUGUI playerWinsText;
    public TextMeshProUGUI botWinsText;
    public Button startBattleButton;
    public GameObject caseButtonPrefab;
    public Transform caseSelectorPanel;
    public Button closeSelectorPanelButton;
    public Button selectCaseButton;
    public TMP_Dropdown gameModeDropdown;
    public float easingDuration = 7f;
    public int numberOfReelItems = 40;
    
    private bool _isScrolling;
    private GridLayoutGroup _gridLayout;
    private RectTransform _playerReelTransform;
    private RectTransform _botReelTransform;
    private Vector3 _initialReelPosition;
    private int _openedItemIndex;
    
    private bool _isSelectorOpen;
    private bool _isFirstSelection = true;
    private List<CaseData> _availableCases;
    private CaseData _selectedCaseData;
    
    private List<ItemData> _playerItems = new List<ItemData>();
    private List<ItemData> _botItems = new List<ItemData>();
    private float _playerTotalValue;
    private float _botTotalValue;
    private bool _isGreaterMode; // true if "greater total value wins"
    
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

    void Start()
    {
        startBattleButton.interactable = false;
        _availableCases = new List<CaseData>(Resources.LoadAll<CaseData>("CaseAssets"));
        
        DisplayCaseSelector(_availableCases);
        selectCaseButton.onClick.AddListener(ToggleCaseSelector);
        closeSelectorPanelButton.onClick.AddListener(CloseCaseSelector);
        
        startBattleButton.onClick.AddListener(StartBattle);
        gameModeDropdown.onValueChanged.AddListener(delegate { SetGameMode(); });

        _gridLayout = playerReelParent.GetComponent<GridLayoutGroup>();
        _playerReelTransform = playerReelParent.GetComponent<RectTransform>();
        
        _gridLayout = botReelParent.GetComponent<GridLayoutGroup>();
        _botReelTransform = botReelParent.GetComponent<RectTransform>();
        
        SetInitialReelPosition(playerReelParent);
        SetInitialReelPosition(botReelParent);
    }

    void SetGameMode()
    {
        _isGreaterMode = gameModeDropdown.value == 0; // "Greater Total Value Wins" = 0, "Lesser Total Value Wins" = 1
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private void SelectCase(CaseData chosenCase)
    {
        _selectedCaseData = chosenCase;
        

        if (_selectedCaseData) startBattleButton.interactable = true;
        else startBattleButton.interactable = false;

        if (_selectedCaseData.price <= PlayerManager.Instance.GetPlayerBalance()) startBattleButton.interactable = true;
        else startBattleButton.interactable = false;
        

        if (_isFirstSelection)
        {
            _isFirstSelection = false;
            _isSelectorOpen = false;
            caseSelectorPanel.gameObject.SetActive(false);
            closeSelectorPanelButton.gameObject.SetActive(false);
        }
        else
        {
            ToggleCaseSelector();
        }
    }
    
    public void DisplayCaseSelector(List<CaseData> availableCases)
    {
        foreach (Transform child in caseSelectorPanel)
        {
            Destroy(child.gameObject);
        }

        List<CaseData> orderedAvailableCases = availableCases.OrderBy(i => i.price).ToList();

        foreach (var caseData in orderedAvailableCases)
        {
            GameObject caseButton = Instantiate(caseButtonPrefab, caseSelectorPanel);
            Image caseImage = caseButton.transform.Find("CaseImage").GetComponent<Image>();
            TextMeshProUGUI nameText = caseButton.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI priceText = caseButton.transform.Find("PriceText").GetComponent<TextMeshProUGUI>();
            
            caseImage.sprite = Resources.Load<Sprite>($"CaseImages/{caseData.id}");
            nameText.text = caseData.name;
            priceText.text = $"{caseData.price:F2}";

            Button button = caseButton.GetComponent<Button>();
            button.onClick.AddListener(() => SelectCase(caseData));
        }
    }
    
    public void ToggleCaseSelector()
    {
        _isSelectorOpen = !_isSelectorOpen;
        caseSelectorPanel.gameObject.SetActive(_isSelectorOpen);
        closeSelectorPanelButton.gameObject.SetActive(_isSelectorOpen);

        if (_isFirstSelection && !_isSelectorOpen)
        {
            _isSelectorOpen = true;
            caseSelectorPanel.gameObject.SetActive(true);
            closeSelectorPanelButton.gameObject.SetActive(true);
        }
    }

    public void CloseCaseSelector()
    {
        _isSelectorOpen = false;
        caseSelectorPanel.gameObject.SetActive(false);
        closeSelectorPanelButton.gameObject.SetActive(false);
    }
    
    void StartBattle()
    {
        if (_selectedCaseData == null)
        {
            Debug.LogError("No case selected. Please select a case.");
            return;
        }
        
        // Clear previous results
        foreach (Transform child in playerResultArea)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in botResultArea)
        {
            Destroy(child.gameObject);
        }

        if (PlayerManager.Instance.GetPlayerBalance() >= _selectedCaseData.price)
        {
            PlayerManager.Instance.DeductCurrency(_selectedCaseData.price);
        }
        else
        {
            Debug.LogWarning("Not enough balance to start battle.");
            return;
        }

        _playerItems.Clear();
        _botItems.Clear();
        _playerTotalValue = 0f;
        _botTotalValue = 0f;
        playerWinsText.text = null;
        botWinsText.text = null;
        startBattleButton.interactable = false;
        selectCaseButton.interactable = false;
        closeSelectorPanelButton.interactable = false;
        gameModeDropdown.interactable = false;
        uiManager.LockUI();

        foreach (Transform child in playerReelParent) Destroy(child.gameObject);
        foreach (Transform child in botReelParent) Destroy(child.gameObject);
        foreach (Transform child in playerResultArea) Destroy(child.gameObject);
        foreach (Transform child in botResultArea) Destroy(child.gameObject);

        StartCoroutine(PlayRounds(5));
    }
    private void SetInitialReelPosition(Transform reelParent)
    {
        float itemWidth = _gridLayout.cellSize.x + _gridLayout.spacing.x;
        float totalReelWidth = itemWidth * numberOfReelItems;
        
        _initialReelPosition = new Vector3(totalReelWidth / 2 - 800, reelParent.localPosition.y, reelParent.localPosition.z);
        reelParent.localPosition = _initialReelPosition;
    }
    
    // ReSharper disable Unity.PerformanceAnalysis
    private IEnumerator AnimateScrollingReel(Transform reelParent, RectTransform reelTransform, int openedItemIndex)
    {
        float itemWidth = _gridLayout.cellSize.x + _gridLayout.spacing.x;
        float totalReelWidth = itemWidth * numberOfReelItems;
        reelTransform.sizeDelta = new Vector2(totalReelWidth, reelTransform.sizeDelta.y);

        float randomOffset = Random.Range(0f, 256f);
        float targetPosition = _initialReelPosition.x - (itemWidth * openedItemIndex) + 800 - randomOffset;
        
        float elapsed = 0f;
        while (elapsed < easingDuration)
        {
            float t = elapsed / easingDuration;
            float easeFactor = EaseOutQuint(t);
            
            float currentPosition = Mathf.Lerp(_initialReelPosition.x, targetPosition, easeFactor);
            reelParent.localPosition = new Vector3(currentPosition, reelParent.localPosition.y, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        reelParent.localPosition = new Vector3(targetPosition, reelParent.localPosition.y, 0);
    }
    
    private float EaseOutQuint(float t)
    {
        return 1 - Mathf.Pow(1 - t, 5);
    }
    
    // ReSharper disable Unity.PerformanceAnalysis
    private IEnumerator PlayRounds(int numberOfRounds)
    {
        for (int i = 0; i < numberOfRounds; i++)
        {
            Debug.Log($"Starting round {i + 1}/{numberOfRounds}");

            // Randomly select items for player and bot
            int playerOpenedItemIndex = Random.Range(24, numberOfReelItems - 4);
            int botOpenedItemIndex = Random.Range(24, numberOfReelItems - 4);

            ItemData playerSelectedItem = GetRandomItemByChance();
            ItemData botSelectedItem = GetRandomItemByChance();

            // Initialize reels
            SetInitialReelPosition(playerReelParent);
            SetInitialReelPosition(botReelParent);

            // Populate reels
            float itemSize = _gridLayout.cellSize.y + _gridLayout.spacing.y;
            PopulateReel(playerReelParent, _playerItems, playerSelectedItem, playerOpenedItemIndex, itemSize);
            PopulateReel(botReelParent, _botItems, botSelectedItem, botOpenedItemIndex, itemSize);

            // Animate reels
            yield return StartCoroutine(AnimateScrollingReel(playerReelParent, _playerReelTransform, playerOpenedItemIndex));
            DisplayPlayerRoundResult(playerSelectedItem);
            
            yield return StartCoroutine(AnimateScrollingReel(botReelParent, _botReelTransform, botOpenedItemIndex));
            DisplayBotRoundResult(botSelectedItem);
        }

        // Determine the overall winner
        DetermineWinner();
    }
    
    private void PopulateReel(Transform reelParent, List<ItemData> reelItems, ItemData selectedItem, int openedItemIndex, float itemSize)
    {
        // Clear previous items
        foreach (Transform child in reelParent)
        {
            Destroy(child.gameObject);
        }

        Debug.Log("Populating reel...");
        for (int i = 0; i < numberOfReelItems; i++)
        {
            GameObject reelItem = Instantiate(reelItemPrefab, reelParent);
            ItemData itemData = (i == openedItemIndex) ? selectedItem : GetRandomItemByChance();
            SetUpReelItem(reelItem, itemData);

            // Log reel item details
            Debug.Log($"Reel Item {i}: {itemData.name} (ID: {itemData.id}, Price: {itemData.price}, Rarity: {itemData.rarity})");
        }

        Debug.Log($"Selected item (OpenedItemIndex {openedItemIndex}): {selectedItem.name} (ID: {selectedItem.id})");
    }
    
    private void DisplayPlayerRoundResult(ItemData playerItem)
    {
        Debug.Log($"Player Backend-Picked Item: {playerItem.name} (ID: {playerItem.id})");

        // Display Player's Result
        GameObject playerResultItem = Instantiate(resultPrefab, playerResultArea);
        SetUpResultItem(playerResultItem, playerItem);
        _playerTotalValue += playerItem.price;
        playerTotalValueText.text = $"Total Value: {_playerTotalValue:F2}€";
    }
    
    private void DisplayBotRoundResult(ItemData botItem)
    {
        Debug.Log($"Bot Backend-Picked Item: {botItem.name} (ID: {botItem.id})");

        // Display Bot's Result
        GameObject botResultItem = Instantiate(resultPrefab, botResultArea);
        SetUpResultItem(botResultItem, botItem);
        _botTotalValue += botItem.price;
        botTotalValueText.text = $"Total Value: {_botTotalValue:F2}€";
    }
    
    private void SetUpResultItem(GameObject resultItem, ItemData itemData)
    {
        Image itemImage = resultItem.transform.Find("ItemImage").GetComponent<Image>();
        Image rarityImage = resultItem.transform.Find("RarityImage").GetComponent<Image>();
        TextMeshProUGUI gunText = resultItem.transform.Find("GunText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI nameText = resultItem.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI priceText = resultItem.transform.Find("PriceText").GetComponent<TextMeshProUGUI>();

        itemImage.sprite = Resources.Load<Sprite>($"ItemImages/{itemData.id}");
        rarityImage.sprite = Resources.Load<Sprite>($"RarityImages/{itemData.rarity}");
        gunText.text = itemData.gun;
        nameText.text = itemData.name;
        priceText.text = $"{itemData.price:F2}€";
    }

    private void SetUpReelItem(GameObject reelItem, ItemData itemData)
    {
        Image itemImage = reelItem.transform.Find("ItemImage").GetComponent<Image>();
        Image rarityImage = reelItem.transform.Find("RarityImage").GetComponent<Image>();
        
        itemImage.sprite = Resources.Load<Sprite>($"ItemImages/{itemData.id}");
        rarityImage.sprite = Resources.Load<Sprite>($"RarityImages/{itemData.rarity}");
    }
    
    private void DetermineWinner()
    {
        Debug.Log($"Player Total Value: {_playerTotalValue:F2}");
        Debug.Log($"Bot Total Value: {_botTotalValue:F2}");

        if (gameModeDropdown.options[gameModeDropdown.value].text == "Higher Wins")
        {
            string result = _playerTotalValue > _botTotalValue ? "Player Wins!" : "Bot Wins!"; 
            TextMeshProUGUI winningText = _playerTotalValue > _botTotalValue ? playerWinsText : botWinsText;
            winningText.text = result;
            if (_playerTotalValue > _botTotalValue)
            {
                PlayerManager.Instance.AddCurrency(_playerTotalValue);
            }
            Debug.Log(result);
        }
        else if (gameModeDropdown.options[gameModeDropdown.value].text == "Lower Wins")
        {
            string result = _playerTotalValue < _botTotalValue ? "Player Wins!" : "Bot Wins!";
            TextMeshProUGUI winningText = _playerTotalValue < _botTotalValue ? playerWinsText : botWinsText;
            winningText.text = result;
            if (_playerTotalValue < _botTotalValue)
            {
                PlayerManager.Instance.AddCurrency(_botTotalValue);
            }
            Debug.Log(result);
        }
        else
        {
            playerWinsText.text = "Tie!";
            botWinsText.text = "Tie!";
            PlayerManager.Instance.AddCurrency(_selectedCaseData.price);
            Debug.Log("Tie!");
        }
        
        startBattleButton.interactable = true;
        selectCaseButton.interactable = true;
        closeSelectorPanelButton.interactable = true;
        gameModeDropdown.interactable = true;
        uiManager.UnlockUI();
    }

    // ReSharper disable Unity.PerformanceAnalysis
    ItemData GetRandomItemByChance()
    {
        if (_selectedCaseData.items == null || _selectedCaseData.items.Count == 0)
        {
            Debug.LogError("No items in the selected case.");
            return null;
        }

        float totalWeight = 0f;
        Dictionary<ItemData, float> itemWeights = new Dictionary<ItemData, float>();
        
        foreach (var item in _selectedCaseData.items)
        {
            if (!RarityWeights.TryGetValue(item.rarity, out float baseWeight))
            {
                Debug.LogWarning($"Unknown rarity '{item.rarity}' for item '{item.id}'");
                continue;
            }
            
            float itemWeight = item.isStatTrak ? baseWeight / 10 : baseWeight;
            itemWeights[item] = itemWeight;
            totalWeight += itemWeight;
        }

        if (totalWeight <= 0)
        {
            Debug.LogError("Total weight is zero or negative. Ensure items have valid rarities and weights.");
            return null;
        }
        
        float randomValue = Random.Range(0f, totalWeight);
        float cumulativeWeight = 0f;

        foreach (var itemWeightPair in itemWeights)
        {
            cumulativeWeight += itemWeightPair.Value;
            if (randomValue <= cumulativeWeight)
            {
                return itemWeightPair.Key;
            }
        }

        Debug.LogWarning("No item was selected; returning default item.");
        return _selectedCaseData.items.FirstOrDefault();
    }
}
