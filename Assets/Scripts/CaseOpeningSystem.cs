using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Random = UnityEngine.Random;

public class CaseOpening : MonoBehaviour
{
    public GameObject caseItemPrefab;
    public GameObject caseSpecialItemPrefab;
    public Transform caseItemGrid;
    public GameObject openedItemPrefab;
    public Transform caseGridParent;
    public int numberOfReelItems = 40;
    public float easingDuration = 7f;
    
    private bool _isScrolling;
    private Vector3 _initialReelPosition;
    private GridLayoutGroup _gridLayout;
    private RectTransform _reelTransform;
    private int _openedItemIndex;
    private float _randomOffset;

    public GameObject caseButtonPrefab;
    public Transform caseSelectorPanel;
    public Button normalCasesButton;
    public Button customCasesButton;
    public Button selectCaseButton;
    public Button openCaseButton;

    private bool _isSelectorOpen;
    private bool _isFirstSelection = true;
    private List<CaseData> _availableCases;
    private CaseData _selectedCaseData;

    public UIManager uiManager;

    private Dictionary<string, List<ItemData>> _rarityGroups;

    void Start()
    {   
        openCaseButton.interactable = false;
        _availableCases = new List<CaseData>(Resources.LoadAll<CaseData>("CaseAssets"));
        DisplayCaseSelector(_availableCases);
        
        selectCaseButton.onClick.AddListener(ToggleCaseSelector);
        normalCasesButton.onClick.AddListener(ChangeToNormalCase);
        customCasesButton.onClick.AddListener(ChangeToCustomCase);

        _gridLayout = caseGridParent.GetComponent<GridLayoutGroup>();
        _reelTransform = caseGridParent.GetComponent<RectTransform>();

        SetInitialReelPosition();
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
    
    private void GroupItemsByRarity()
    {
        _rarityGroups = _selectedCaseData.items
            .Where(item => item != null)
            .GroupBy(item => item.rarity)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private void SelectCase(CaseData chosenCase)
    {
        _selectedCaseData = chosenCase;
        
        GroupItemsByRarity();

        if (_selectedCaseData) openCaseButton.interactable = true;
        else openCaseButton.interactable = false;

        if (_selectedCaseData.price <= PlayerManager.Instance.GetPlayerBalance()) openCaseButton.interactable = true;
        else openCaseButton.interactable = false;
        
        DisplayCaseItems(_selectedCaseData);

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

    private void SetInitialReelPosition()
    {
        float itemWidth = _gridLayout.cellSize.x + _gridLayout.spacing.x;
        float totalReelWidth = itemWidth * numberOfReelItems;
        
        _initialReelPosition = new Vector3(totalReelWidth / 2 - 800, caseGridParent.localPosition.y, caseGridParent.localPosition.z);
        caseGridParent.localPosition = _initialReelPosition;
    }

    public void OpenCase()
    {
        if (_isScrolling) return;

        if (PlayerManager.Instance == null)
        {
            Debug.LogError("PlayerManager is null");
            return;
        }

        if (_selectedCaseData == null)
        {
            Debug.LogError("No case selected. Please select a case first.");
            return;
        }

        foreach (Transform child in caseGridParent) 
        {
            Destroy(child.gameObject);
        }

        caseGridParent.localPosition = _initialReelPosition;

        if (PlayerManager.Instance.GetPlayerBalance() >= _selectedCaseData.price)
        {
            PlayerManager.Instance.DeductCurrency(_selectedCaseData.price);
            ItemData openedItem = GetRandomItemByPercentage();
            StartCoroutine(AnimateScrollingReel(openedItem));
        }
        else
        {
            Debug.LogError("Not enough money to open this case.");
        }
    }

    // ReSharper disable Unity.PerformanceAnalysis
    public ItemData GetRandomItemByPercentage()
    {
        if (_selectedCaseData.items == null || _selectedCaseData.items.Count == 0)
        {
            Debug.LogError("No items in the selected case. Ensure the selected case has items.");
            return null;
        }
        
        if (_rarityGroups.Count == 0)
        {
            Debug.LogError("No valid rarity groups found in the selected case.");
            return null;
        }

        float totalRarityWeight = RarityWeights.WeightList
            .Where(rarity => _rarityGroups.ContainsKey(rarity.Key))
            .Sum(rarity => rarity.Value);

        float rarityRandomValue = Random.Range(0, totalRarityWeight);
        
        float cumulativeRarityWeight = 0f;
        string selectedRarity = null;

        foreach (var rarity in RarityWeights.WeightList)
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

        Debug.LogWarning("No opened item was selected; returning null.");
        return null;
    }
    
    private ItemData GetRandomNonSpecialItem()
    {
        List<ItemData> nonSpecialItems = _selectedCaseData.items
            .Where(item => item != null && item.rarity != "SPECIAL")
            .ToList();

        if (nonSpecialItems == null || nonSpecialItems.Count == 0)
        {
            Debug.LogError("No valid non-special items in the selected case.");
            return null;
        }
        
        float totalRarityWeight = RarityWeights.WeightList
            .Where(rarity => rarity.Key != "SPECIAL")
            .Sum(rarity => rarity.Value);

        if (totalRarityWeight <= 0)
        {
            Debug.LogError("Total weight for non-special rarities is invalid.");
            return null;
        }
        
        float rarityRandomValue = Random.Range(0, totalRarityWeight);

        float cumulativeRarityWeight = 0f;
        string selectedRarity = null;
        
        foreach (var rarity in RarityWeights.WeightList)
        {
            if (rarity.Key == "SPECIAL") continue;
            
            cumulativeRarityWeight += rarity.Value;
            if (rarityRandomValue <= cumulativeRarityWeight && rarity.Key != "SPECIAL")
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
        
        Debug.LogWarning("No random item was selected; returning default item.");
        return _selectedCaseData.items[0];
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private IEnumerator AnimateScrollingReel(ItemData openedItem)
    {
        _isScrolling = true;
        openCaseButton.interactable = false;
        selectCaseButton.interactable = false;
        normalCasesButton.interactable = false;
        customCasesButton.interactable = false;
        uiManager.LockUI();

        List<GameObject> reelItems = new List<GameObject>();
        if (reelItems == null) throw new ArgumentNullException(nameof(reelItems));
        float itemWidth = _gridLayout.cellSize.x + _gridLayout.spacing.x;
        float totalReelWidth = itemWidth * numberOfReelItems;
        _reelTransform.sizeDelta = new Vector2(totalReelWidth, _reelTransform.sizeDelta.y);

        _openedItemIndex = Random.Range(24, numberOfReelItems - 4);
        for (int i = 0; i < numberOfReelItems; i++)
        {
            ItemData itemData;

            if (i == _openedItemIndex)
            {
                itemData = openedItem;
            }
            else
            {
                if (_rarityGroups.Count == 1 && _rarityGroups.ContainsKey("SPECIAL"))
                {
                    itemData = GetRandomItemByPercentage();
                }
                else
                {
                    itemData = GetRandomNonSpecialItem();
                }
            }
            GameObject reelItem = Instantiate(openedItemPrefab, caseGridParent);
            SetUpReelItem(reelItem, itemData);
            reelItems.Add(reelItem);
        }

        _randomOffset = Random.Range(0f, 256f);
        float targetPosition = _initialReelPosition.x - (itemWidth * _openedItemIndex) + 800 - _randomOffset;

        float elapsed = 0f;
        while (elapsed < easingDuration)
        {
            float t = elapsed / easingDuration;
            float easeFactor = EaseOutQuint(t);

            float currentPosition = Mathf.Lerp(_initialReelPosition.x, targetPosition, easeFactor);
            caseGridParent.localPosition = new Vector3(currentPosition, caseGridParent.localPosition.y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        caseGridParent.localPosition = new Vector3(targetPosition, caseGridParent.localPosition.y, 0);

        StopReel(openedItem);
    }

    private void StopReel(ItemData openedItem)
    {
        InventoryManager.Instance.AddItemToInventory(openedItem);
        CollectionManager.Instance.AddItemToCollection(openedItem);
        if (PlayerManager.Instance.GetPlayerBalance() < _selectedCaseData.price)
        {
            DisplayCaseSelector(_availableCases);
            ToggleCaseSelector();
            openCaseButton.interactable = false;
        }
        else
        {
            openCaseButton.interactable = true;
        }
        _isScrolling = false;
        selectCaseButton.interactable = true;
        normalCasesButton.interactable = true;
        customCasesButton.interactable = true;
        uiManager.UnlockUI();
    }

    private void SetUpReelItem(GameObject reelItem, ItemData itemData)
    {
        Image itemImage = reelItem.transform.Find("ItemImage").GetComponent<Image>();
        Image rarityImage = reelItem.transform.Find("RarityImage").GetComponent<Image>();

        string imagePath = "ItemImages/" + itemData.id;
        string rarityPath = "RarityImages/" + itemData.rarity;

        itemImage.sprite = Resources.Load<Sprite>(imagePath);
        rarityImage.sprite = Resources.Load<Sprite>(rarityPath);
    }
    
    private float EaseOutQuint(float t)
    {
        return 1 - Mathf.Pow(1 - t, 5);
    }

    public void DisplayCaseItems(CaseData selectedCase)
    {
        if (caseItemGrid == null)
        {
            Debug.LogError("caseItemGrid is not assigned in the Inspector.");
            return;
        }
        
        foreach (Transform child in caseItemGrid)
        {
            Destroy(child.gameObject);
        }
        
        bool hasSpecialItem = false;
        float minSpecialPrice = float.MaxValue;
        float maxSpecialPrice = float.MinValue;

        var groupedItems = selectedCase.items
            .Where(item => item != null)
            .GroupBy(item =>
            {
                string itemIndex = item.id.Split('_')[1].TrimEnd('B', 'W', 'F', 'T', 'N', 'M', 'S');
                return itemIndex;
            });

        foreach (var group in groupedItems)
        {
            var itemsInGroup = group.ToList();
            float minPrice = itemsInGroup.Min(item=> item.price);
            float maxPrice = itemsInGroup.Max(item => item.price);
            
            var bestConditionItem = itemsInGroup
                .Where(item => item.rarity != "SPECIAL")
                .OrderByDescending(item => ConditionOrder.ConditionOrderList.TryGetValue(item.condition, out int order) ? order : 0)
                .FirstOrDefault();

            if (bestConditionItem != null)
            {
                GameObject item = Instantiate(caseItemPrefab, caseItemGrid);
                Image itemImage = item.transform.Find("ItemImage").GetComponent<Image>();
                Image rarityImage = item.transform.Find("RarityImage").GetComponent<Image>();
                TextMeshProUGUI gunText = item.transform.Find("GunText").GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI nameText = item.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI priceText = item.transform.Find("PriceText").GetComponent<TextMeshProUGUI>();

                itemImage.sprite = Resources.Load<Sprite>($"ItemImages/{bestConditionItem.id}");
                rarityImage.sprite = Resources.Load<Sprite>($"RarityImages/{bestConditionItem.rarity}");
                gunText.text = bestConditionItem.gun;
                nameText.text = bestConditionItem.name;
                priceText.text = $"{minPrice:F2}€ - {maxPrice:F2}€";
            }

            if (itemsInGroup.Any(item => item.rarity == "SPECIAL"))
            {
                hasSpecialItem = true;
                minSpecialPrice = Mathf.Min(minSpecialPrice, minPrice);
                maxSpecialPrice = Mathf.Max(maxSpecialPrice, maxPrice);
            }
        }

        if (hasSpecialItem)
        {
            GameObject specialItem = Instantiate(caseSpecialItemPrefab, caseItemGrid);
            Image itemImage = specialItem.transform.Find("ItemImage").GetComponent<Image>();
            Image rarityImage = specialItem.transform.Find("RarityImage").GetComponent<Image>();
            TextMeshProUGUI nameText = specialItem.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI priceText = specialItem.transform.Find("PriceText").GetComponent<TextMeshProUGUI>();

            itemImage.sprite = Resources.Load<Sprite>($"OtherImages/special_item");
            rarityImage.sprite = Resources.Load<Sprite>($"RarityImages/SPECIAL");
            nameText.text = "Special Item";
            priceText.text = $"{minSpecialPrice:F2}€ - {maxSpecialPrice:F2}€";
        }
    }

    public void ToggleCaseSelector()
    {
        _isSelectorOpen = !_isSelectorOpen;
        caseSelectorPanel.gameObject.SetActive(_isSelectorOpen);
        openCaseButton.interactable = !_isSelectorOpen;

        if (_isFirstSelection && !_isSelectorOpen)
        {
            _isSelectorOpen = true;
            caseSelectorPanel.gameObject.SetActive(true);
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
            priceText.text = $"{caseData.price:F2}€";

            Button button = caseButton.GetComponent<Button>();
            button.onClick.AddListener(() => SelectCase(caseData));
        }
    }
}
