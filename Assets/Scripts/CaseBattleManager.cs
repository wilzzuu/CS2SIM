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
    public TextMeshProUGUI winnerText;
    public Button startBattleButton;
    public GameObject caseButtonPrefab;
    public Transform caseSelectorPanel;
    public Button selectCaseButton;
    public TMP_Dropdown gameModeDropdown;
    public float spinDuration = 7f;
    public int numberOfReelItems = 40;
    
    private bool _isScrolling;
    private GridLayoutGroup _gridLayout;
    private RectTransform _reelTransform;
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
        
        startBattleButton.onClick.AddListener(StartBattle);
        gameModeDropdown.onValueChanged.AddListener(delegate { SetGameMode(); });

        _gridLayout = playerReelParent.GetComponent<GridLayoutGroup>();
        _reelTransform = playerReelParent.GetComponent<RectTransform>();
        
        _gridLayout = botReelParent.GetComponent<GridLayoutGroup>();
        _reelTransform = botReelParent.GetComponent<RectTransform>();
        
        SetInitialReelPosition(playerReelParent, numberOfReelItems, 256f);
        SetInitialReelPosition(botReelParent, numberOfReelItems, 256f);
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

        if (_isFirstSelection && !_isSelectorOpen)
        {
            _isSelectorOpen = true;
            caseSelectorPanel.gameObject.SetActive(true);
        }
    }
    
    void StartBattle()
    {
        if (_selectedCaseData == null)
        {
            Debug.LogError("No case selected. Please select a case.");
            return;
        }

        _playerItems.Clear();
        _botItems.Clear();
        _playerTotalValue = 0f;
        _botTotalValue = 0f;

        foreach (Transform child in playerReelParent) Destroy(child.gameObject);
        foreach (Transform child in botReelParent) Destroy(child.gameObject);
        foreach (Transform child in playerResultArea) Destroy(child.gameObject);
        foreach (Transform child in botResultArea) Destroy(child.gameObject);

        StartCoroutine(PlayRounds(5, 256f));
    }
    
    private void SetInitialReelPosition(Transform reelParent, int numberOfItems, float itemSize)
    {
        RectTransform reelRect = reelParent.GetComponent<RectTransform>();
        float totalReelHeight = numberOfItems * itemSize;
        reelRect.sizeDelta = new Vector2(reelRect.sizeDelta.x, totalReelHeight);

        // Position reel at the start
        reelRect.localPosition = new Vector3(reelRect.localPosition.x, 0, reelRect.localPosition.z);
        Debug.Log($"Reel initialized: Height={totalReelHeight}, Position={reelRect.localPosition}");
    }
    
    // ReSharper disable Unity.PerformanceAnalysis
    private IEnumerator AnimateScrollingReel(Transform reelParent, int openedItemIndex, float itemSize, float duration)
    {
        RectTransform reelRect = reelParent.GetComponent<RectTransform>();
        float totalReelHeight = itemSize * numberOfReelItems;
        float reelViewportHeight = reelRect.parent.GetComponent<RectTransform>().rect.height;

        // Calculate target position for the selected item
        float targetPositionY = -(openedItemIndex * itemSize) + (reelViewportHeight / 2) - (itemSize / 2);
        targetPositionY = Mathf.Clamp(targetPositionY, -totalReelHeight + reelViewportHeight, 0);

        Debug.Log($"Animating reel: Start={reelRect.localPosition.y}, Target={targetPositionY}");

        // Animate reel movement
        Vector3 startPosition = reelRect.localPosition;
        Vector3 targetPosition = new Vector3(startPosition.x, targetPositionY, startPosition.z);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float easeFactor = EaseOutQuint(t);
            reelRect.localPosition = Vector3.Lerp(startPosition, targetPosition, easeFactor);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Snap to final position
        reelRect.localPosition = targetPosition;
        Debug.Log($"Reel animation complete. Final Position: {reelRect.localPosition}");
    }
    
    private float EaseOutQuint(float t)
    {
        return 1 - Mathf.Pow(1 - t, 5);
    }
    
    // ReSharper disable Unity.PerformanceAnalysis
    private IEnumerator PlayRounds(int numberOfRounds, float itemSize)
    {
        for (int i = 0; i < numberOfRounds; i++)
        {
            Debug.Log($"Starting round {i + 1}/{numberOfRounds}");

            // Deduct the cost of the case
            PlayerManager.Instance.DeductCurrency(_selectedCaseData.price);

            // Randomly select items for player and bot
            int playerOpenedItemIndex = Random.Range(24, numberOfReelItems - 4);
            int botOpenedItemIndex = Random.Range(24, numberOfReelItems - 4);
            ItemData playerSelectedItem = GetRandomItemByChance();
            ItemData botSelectedItem = GetRandomItemByChance();

            // Initialize reels
            SetInitialReelPosition(playerReelParent, numberOfReelItems, itemSize);
            SetInitialReelPosition(botReelParent, numberOfReelItems, itemSize);

            // Populate reels
            PopulateReel(playerReelParent, _playerItems, playerSelectedItem, playerOpenedItemIndex, itemSize);
            PopulateReel(botReelParent, _botItems, botSelectedItem, botOpenedItemIndex, itemSize);

            // Animate reels
            yield return StartCoroutine(AnimateScrollingReel(playerReelParent, playerOpenedItemIndex, itemSize, spinDuration));
            yield return StartCoroutine(AnimateScrollingReel(botReelParent, botOpenedItemIndex, itemSize, spinDuration));

            // Display results
            DisplayRoundResult(playerSelectedItem, botSelectedItem);

            // Update total values
            _playerTotalValue += playerSelectedItem.price;
            _botTotalValue += botSelectedItem.price;
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

        // Populate with items
        for (int i = 0; i < numberOfReelItems; i++)
        {
            GameObject reelItem = Instantiate(reelItemPrefab, reelParent);
            ItemData itemData = (i == openedItemIndex) ? selectedItem : GetRandomItemByChance();
            SetUpReelItem(reelItem, itemData);

            // Set vertical position
            RectTransform itemRect = reelItem.GetComponent<RectTransform>();
            itemRect.anchoredPosition = new Vector2(0, -i * itemSize);
        }

        Debug.Log($"Reel populated with {numberOfReelItems} items. Selected index: {openedItemIndex}");
    }
    
    private void DisplayRoundResult(ItemData playerItem, ItemData botItem)
    {
        // Clear previous results
        foreach (Transform child in playerResultArea)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in botResultArea)
        {
            Destroy(child.gameObject);
        }

        // Display Player's Result
        GameObject playerResultItem = Instantiate(resultPrefab, playerResultArea);
        SetUpResultItem(playerResultItem, playerItem);

        // Display Bot's Result
        GameObject botResultItem = Instantiate(resultPrefab, botResultArea);
        SetUpResultItem(botResultItem, botItem);
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
            Debug.Log(result);
        }
        else if (gameModeDropdown.options[gameModeDropdown.value].text == "Lower Wins")
        {
            string result = _playerTotalValue < _botTotalValue ? "Player Wins!" : "Bot Wins!";
            Debug.Log(result);
        }
    }

    // ReSharper disable Unity.PerformanceAnalysis
    ItemData GetRandomItemByChance()
    {
        if (_selectedCaseData.items == null || _selectedCaseData.items.Count == 0)
        {
            Debug.LogError("No items in the selected case.");
            return null;
        }

        float totalWeight = _selectedCaseData.items
            .Where(item => RarityWeights.ContainsKey(item.rarity))
            .Sum(item => RarityWeights[item.rarity]);

        float randomValue = Random.Range(0, totalWeight);
        float cumulativeWeight = 0f;

        foreach (var item in _selectedCaseData.items)
        {
            if (RarityWeights.TryGetValue(item.rarity, out float weight))
            {
                cumulativeWeight += weight;
                if (randomValue <= cumulativeWeight)
                {
                    return item;
                }
            }
        }

        Debug.LogWarning("No item was selected; returning default item.");
        return _selectedCaseData.items.FirstOrDefault();
    }
}
