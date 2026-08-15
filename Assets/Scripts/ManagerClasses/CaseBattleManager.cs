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
    public List<GameObject> playerSlots = new();
    public Transform botResultArea;
    public List<GameObject> botSlots = new();
    public TextMeshProUGUI playerTotalValueText;
    public TextMeshProUGUI botTotalValueText;
    public TextMeshProUGUI playerWinsText;
    public TextMeshProUGUI botWinsText;
    public Button startBattleButton;
    public GameObject caseButtonPrefab;
    public Transform caseSelectorPanel;
    public Button closeSelectorPanelButton;
    public Button normalCasesButton;
    public Button customCasesButton;
    public Button selectCaseButton;
    public TMP_Dropdown gameModeDropdown;
    public float easingDuration = 7f;
    public int numberOfReelItems = 40;
    public float keyPrice = 2.19f;
    
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
    private bool _isGreaterMode;
    private Dictionary<string, List<ItemData>> _rarityGroups;
    
    public UIManager uiManager;

    void Start()
    {
        startBattleButton.interactable = false;
        _availableCases = new List<CaseData>(Resources.LoadAll<CaseData>("CaseAssets"));
        DisplayCaseSelector(_availableCases);
        
        selectCaseButton.onClick.AddListener(ToggleCaseSelector);
        closeSelectorPanelButton.onClick.AddListener(CloseCaseSelector);
        normalCasesButton.onClick.AddListener(ChangeToNormalCase);
        customCasesButton.onClick.AddListener(ChangeToCustomCase);
        
        startBattleButton.onClick.AddListener(StartBattle);
        gameModeDropdown.onValueChanged.AddListener(delegate { SetGameMode(); });

        _gridLayout = playerReelParent.GetComponent<GridLayoutGroup>();
        _playerReelTransform = playerReelParent.GetComponent<RectTransform>();
        
        _gridLayout = botReelParent.GetComponent<GridLayoutGroup>();
        _botReelTransform = botReelParent.GetComponent<RectTransform>();
        
        SetInitialReelPosition(playerReelParent);
        SetInitialReelPosition(botReelParent);

        SetUncoveredResultItems();
    }

    public void ChangeToNormalCase()
    {
        _availableCases = new List<CaseData>(Resources.LoadAll<CaseData>($"CaseAssets"));
        DisplayCaseSelector(_availableCases);
    }

    public void ChangeToCustomCase()
    {
        _availableCases = new List<CaseData>(Resources.LoadAll<CaseData>($"CustomCaseAssets"));
        DisplayCaseSelector(_availableCases);
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
            
            float casePrice = GetCasePrice(caseData);
            
            caseImage.sprite = Resources.Load<Sprite>($"CaseImages/{caseData.id}");
            nameText.text = caseData.name;
            priceText.text = $"{casePrice:N2}€";

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
        
        foreach (Transform child in playerResultArea)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in botResultArea)
        {
            Destroy(child.gameObject);
        }
        
        SetUncoveredResultItems();
        
        float casePrice = GetCasePrice(_selectedCaseData);
        
        if (PlayerManager.Instance.GetPlayerBalance() >= casePrice)
        {
            PlayerManager.Instance.DeductMoney(casePrice);

            LogUtil.Economy.LogEuro(
                "CASE_BATTLE",
                -casePrice,
                PlayerManager.Instance.GetPlayerBalance(),
                $"{_selectedCaseData.name}"
            );
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
        playerTotalValueText.text = "Total Value: 0€";
        botTotalValueText.text = "Total Value: 0€";
        
        playerWinsText.text = null;
        botWinsText.text = null;
        startBattleButton.interactable = false;
        selectCaseButton.interactable = false;
        closeSelectorPanelButton.interactable = false;
        normalCasesButton.interactable = false;
        customCasesButton.interactable = false;
        gameModeDropdown.interactable = false;
        uiManager.LockUI();

        foreach (Transform child in playerReelParent) Destroy(child.gameObject);
        foreach (Transform child in botReelParent) Destroy(child.gameObject);

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
            int playerOpenedItemIndex = Random.Range(24, numberOfReelItems - 4);
            int botOpenedItemIndex = Random.Range(24, numberOfReelItems - 4);

            ItemData playerSelectedItem = GetRandomItemByChance();
            ItemData botSelectedItem = GetRandomItemByChance();

            SetInitialReelPosition(playerReelParent);
            SetInitialReelPosition(botReelParent);

            float itemSize = _gridLayout.cellSize.y + _gridLayout.spacing.y;
            PopulateReel(playerReelParent, _playerItems, playerSelectedItem, playerOpenedItemIndex, itemSize);
            PopulateReel(botReelParent, _botItems, botSelectedItem, botOpenedItemIndex, itemSize);

            yield return StartCoroutine(AnimateScrollingReel(playerReelParent, _playerReelTransform, playerOpenedItemIndex));
            DisplayPlayerRoundResult(playerSelectedItem, i);
            
            yield return StartCoroutine(AnimateScrollingReel(botReelParent, _botReelTransform, botOpenedItemIndex));
            DisplayBotRoundResult(botSelectedItem, i);
        }

        DetermineWinner();
    }
    
    private void PopulateReel(Transform reelParent, List<ItemData> reelItems, ItemData selectedItem, int openedItemIndex, float itemSize)
    {
        foreach (Transform child in reelParent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < numberOfReelItems; i++)
        {
            GameObject reelItem = Instantiate(reelItemPrefab, reelParent);
            ItemData itemData = (i == openedItemIndex) ? selectedItem : GetRandomItemByChance();
            SetUpReelItem(reelItem, itemData); 
        }
    }

    private void SetUncoveredResultItems()
    {
        for (int i = 0; i < 5; i++)
        {
            GameObject playerResultItem = Instantiate(resultPrefab, playerResultArea);
            Image playerUncovered =  playerResultItem.transform.Find("UncoveredImage")?.GetComponent<Image>();
            playerUncovered.enabled = true;

            GameObject botResultItem = Instantiate(resultPrefab, botResultArea);
            Image botUncovered =  botResultItem.transform.Find("UncoveredImage")?.GetComponent<Image>();
            botUncovered.enabled = true;
        }
    }
    
    private void DisplayPlayerRoundResult(ItemData playerItem, int roundNumber)
    {
        GameObject playerResultItem = playerResultArea.GetChild(roundNumber).gameObject;
        SetUpResultItem(playerResultItem, playerItem);
        _playerTotalValue += playerItem.price;
        playerTotalValueText.text = $"Total Value: {_playerTotalValue:N2}€";
    }
    
    private void DisplayBotRoundResult(ItemData botItem, int roundNumber)
    {
        GameObject botResultItem = botResultArea.GetChild(roundNumber).gameObject;
        SetUpResultItem(botResultItem, botItem);
        _botTotalValue += botItem.price;
        botTotalValueText.text = $"Total Value: {_botTotalValue:N2}€";
    }
    
    private void SetUpResultItem(GameObject resultItem, ItemData itemData)
    {
        Image uncoveredImage =  resultItem.transform.Find("UncoveredImage")?.GetComponent<Image>();
        Image itemImage = resultItem.transform.Find("ItemImage").GetComponent<Image>();
        Image rarityImage = resultItem.transform.Find("RarityImage").GetComponent<Image>();
        TextMeshProUGUI gunText = resultItem.transform.Find("GunText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI nameText = resultItem.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI priceText = resultItem.transform.Find("PriceText").GetComponent<TextMeshProUGUI>();

        uncoveredImage.enabled = false;
        itemImage.sprite = Resources.Load<Sprite>($"ItemImages/{itemData.imageName}");
        rarityImage.sprite = Resources.Load<Sprite>($"RarityImages/{itemData.rarity}");
        gunText.text = itemData.gun;
        nameText.text = itemData.name;
        priceText.text = $"{itemData.price:N2}€";
    }

    private void SetUpReelItem(GameObject reelItem, ItemData itemData)
    {
        Image itemImage = reelItem.transform.Find("ItemImage").GetComponent<Image>();
        Image rarityImage = reelItem.transform.Find("RarityImage").GetComponent<Image>();
        TextMeshProUGUI conditionText = reelItem.transform.Find("ConditionText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI statTrakText = reelItem.transform.Find("StatTrakText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI priceText = reelItem.transform.Find("PriceText").GetComponent<TextMeshProUGUI>();
        
        if (conditionText != null)
        {
            conditionText.text = itemData.condition;
        }

        if (statTrakText != null)
        {
            if (itemData.isStatTrak)
            {
                statTrakText.gameObject.SetActive(true);
                statTrakText.text = Globals.StatTrakUIText;
            }
        }
        else
        {
            statTrakText.gameObject.SetActive(false);
        }

        priceText.text = $"{itemData.price:N2}€";

        itemImage.sprite = Resources.Load<Sprite>($"ItemImages/{itemData.imageName}");
        rarityImage.sprite = Resources.Load<Sprite>($"RarityImages/{itemData.rarity}");
    }
    
    private void DetermineWinner()
    {
        float casePrice = GetCasePrice(_selectedCaseData);
        if (gameModeDropdown.options[gameModeDropdown.value].text == "Higher Wins")
        {
            string result = _playerTotalValue > _botTotalValue ? "Player Wins!" : "Bot Wins!"; 
            TextMeshProUGUI winningText = _playerTotalValue > _botTotalValue ? playerWinsText : botWinsText;
            winningText.text = result;
            if (_playerTotalValue > _botTotalValue)
            {
                float _totalGameValue = _playerTotalValue + _botTotalValue;
                PlayerManager.Instance.AddMoney(_totalGameValue);
            }
            else if (Mathf.Approximately(_playerTotalValue, _botTotalValue))
            {
                playerWinsText.text = "Tie!";
                botWinsText.text = "Tie!";
                PlayerManager.Instance.AddMoney(casePrice);
            }
        }
        else if (gameModeDropdown.options[gameModeDropdown.value].text == "Lower Wins")
        {
            string result = _playerTotalValue < _botTotalValue ? "Player Wins!" : "Bot Wins!";
            TextMeshProUGUI winningText = _playerTotalValue < _botTotalValue ? playerWinsText : botWinsText;
            winningText.text = result;
            if (_playerTotalValue < _botTotalValue)
            {
                float _totalGameValue = _playerTotalValue + _botTotalValue;
                PlayerManager.Instance.AddMoney(_totalGameValue);
            }
            else if (Mathf.Approximately(_playerTotalValue, _botTotalValue))
            {
                playerWinsText.text = "Tie!";
                botWinsText.text = "Tie!";
                PlayerManager.Instance.AddMoney(casePrice);
            }
        }
    
        startBattleButton.interactable = true;
        selectCaseButton.interactable = true;
        closeSelectorPanelButton.interactable = true;
        normalCasesButton.interactable = true;
        customCasesButton.interactable = true;
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
            
        _rarityGroups = _selectedCaseData.items
            .Where(item => item != null)
            .GroupBy(item => item.rarity)
            .ToDictionary(g => g.Key, g => g.ToList());
        
        if (_rarityGroups.Count == 0)
        {
            Debug.LogError("No valid rarity groups found in the selected case.");
            return null;
        }
        
        float totalRarityWeight = RarityData.Weight
            .Where(rarity => _rarityGroups.ContainsKey(rarity.Key))
            .Sum(rarity => rarity.Value);
        float rarityRandomValue = Random.Range(0, totalRarityWeight);
        
        float cumulativeRarityWeight = 0f;
        string selectedRarity = null;
        
        foreach (var rarity in RarityData.Weight)
        {
            if (!_rarityGroups.ContainsKey(rarity.Key)) continue;
            cumulativeRarityWeight += rarity.Value;
            if (rarityRandomValue <= cumulativeRarityWeight)
            {
                selectedRarity = rarity.Key;
                break;
            }
        }
        
        if (selectedRarity != null)
        {
            if (_rarityGroups.TryGetValue(selectedRarity, out var itemsInRarity) && itemsInRarity.Count > 0)
            {
                return itemsInRarity[Random.Range(0, itemsInRarity.Count)];
            }
            Debug.LogWarning($"Rarity group '{selectedRarity}' is empty or missing.");
        }

        Debug.LogWarning("No item was selected; returning null.");
        return null;
    }

    private float GetCasePrice(CaseData caseData)
    {
        int amountOfRounds = 5;
        return (caseData.price + keyPrice) * amountOfRounds;
    }
}
