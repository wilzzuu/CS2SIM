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
    public Transform caseItemGrid;
    public GameObject openedItemPrefab;
    public Transform caseGridParent;
    public float initialScrollSpeed = 4000f;
    public float finalScrollSpeed = 100f;
    public int numberOfReelItems = 40;
    public float easingDuasion = 7f;
    
    private bool _isScrolling;
    private Vector3 _initialReelPosition;
    private GridLayoutGroup _gridLayout;
    private RectTransform _reelTransform;
    private int _openedItemIndex;
    private float _randomOffset;

    public GameObject caseButtonPrefab;
    public Transform caseSelectorPanel;
    public Button selectCaseButton;
    public Button openCaseButton;
    private bool _isSelectorOpen;
    private bool _isFirstSelection = true;
    private List<CaseData> _availableCases;
    private CaseData _selectedCaseData;

    public UIManager uiManager;

    private static readonly Dictionary<string, int> RarityOrder = new Dictionary<string, int>
    {
        {"MIL_SPEC", 1},
        {"RESTRICTED", 2},
        {"CLASSIFIED", 3},
        {"COVERT", 4},
        {"SPECIAL", 5}
    };

    void Start()
    {   
        openCaseButton.interactable = false;
        _availableCases = new List<CaseData>(Resources.LoadAll<CaseData>("CaseAssets"));

        DisplayCaseSelector(_availableCases);
        selectCaseButton.onClick.AddListener(ToggleCaseSelector);

        _gridLayout = caseGridParent.GetComponent<GridLayoutGroup>();
        _reelTransform = caseGridParent.GetComponent<RectTransform>();

        SetInitialReelPosition();
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private void SelectCase(CaseData chosenCase)
    {
        _selectedCaseData = chosenCase;

        if (_selectedCaseData) openCaseButton.interactable = true;
        else openCaseButton.interactable = false;

        if (_selectedCaseData.Price <= PlayerManager.Instance.GetPlayerBalance()) openCaseButton.interactable = true;
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

        if (PlayerManager.Instance == null || _selectedCaseData == null)
        {
            Debug.LogError("PlayerManager or selectedCaseData is null");
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

        if (PlayerManager.Instance.GetPlayerBalance() >= _selectedCaseData.Price)
        {
            PlayerManager.Instance.DeductCurrency(_selectedCaseData.Price);
            ItemData openedItem = GetRandomItemByPercentage();
            StartCoroutine(AnimateScrollingReel(openedItem));
        }
        else
        {
            Debug.LogError("Not enough money to open this case.");
        }
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private ItemData GetRandomItemByPercentage()
    {
        if (_selectedCaseData.Items == null || _selectedCaseData.Items.Count == 0)
        {
            Debug.LogError("No items in the selected case. Ensure the selected case has items.");
            return null; 
        }

        float totalWeight = 0f;
        foreach (var item in _selectedCaseData.Items)
        {
            totalWeight += item.Weight;  
        }

        
        float randomValue = Random.Range(0, totalWeight);
        float cumulativeWeight = 0f;

        foreach (var item in _selectedCaseData.Items)
        {
            cumulativeWeight += item.Weight;
            if (randomValue <= cumulativeWeight)
            {
                return item;
            }
        }

        Debug.LogWarning("No item was selected; returning default item.");
        return _selectedCaseData.Items[_selectedCaseData.Items.Count - 1];
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private IEnumerator AnimateScrollingReel(ItemData openedItem)
    {
        _isScrolling = true;
        openCaseButton.interactable = false;
        selectCaseButton.interactable = false;
        uiManager.LockUI();

        List<GameObject> reelItems = new List<GameObject>();
        if (reelItems == null) throw new ArgumentNullException(nameof(reelItems));
        float itemWidth = _gridLayout.cellSize.x + _gridLayout.spacing.x;
        float totalReelWidth = itemWidth * numberOfReelItems;
        _reelTransform.sizeDelta = new Vector2(totalReelWidth, _reelTransform.sizeDelta.y);

        _openedItemIndex = Random.Range(24, numberOfReelItems - 4);
        for (int i = 0; i < numberOfReelItems; i++)
        {
            ItemData itemData = (i == _openedItemIndex) ? openedItem : GetRandomItemByPercentage();
            GameObject reelItem = Instantiate(openedItemPrefab, caseGridParent);
            SetUpReelItem(reelItem, itemData);
            reelItems.Add(reelItem);
        }

        _randomOffset = Random.Range(0f, 256f);
        float targetPosition = _initialReelPosition.x - (itemWidth * _openedItemIndex) + 800 - _randomOffset;

        float elapsed = 0f;

        while (elapsed < easingDuasion)
        {
            float t = elapsed / easingDuasion;
            float easeFactor = EaseOutQuint(t);

            float currentPosition = Mathf.Lerp(_initialReelPosition.x, targetPosition, easeFactor);
            caseGridParent.localPosition = new Vector3(currentPosition, caseGridParent.localPosition.y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        caseGridParent.localPosition = new Vector3(targetPosition, caseGridParent.localPosition.y, 0);

        StopReel(openedItem);
        _isScrolling = false;
        selectCaseButton.interactable = true;
        uiManager.UnlockUI();
        
    }

    private void StopReel(ItemData openedItem)
    {
        InventoryManager.Instance.AddItemToInventory(openedItem);
        CollectionManager.Instance.AddItemToCollection(openedItem);
        if (PlayerManager.Instance.GetPlayerBalance() < _selectedCaseData.Price)
        {
            DisplayCaseSelector(_availableCases);
            ToggleCaseSelector();
            openCaseButton.interactable = false;
        }
        else
        {
            openCaseButton.interactable = true;
        }
    }

    private void SetUpReelItem(GameObject reelItem, ItemData itemData)
    {
        Image itemImage = reelItem.transform.Find("ItemImage").GetComponent<Image>();
        Image rarityImage = reelItem.transform.Find("RarityImage").GetComponent<Image>();

        string imagePath = "ItemImages/" + itemData.ID;
        string rarityPath = "RarityImages/" + itemData.Rarity;

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

        selectedCase.Items = selectedCase.Items.OrderBy(item => RarityOrder[item.Rarity]).ToList();

        foreach (Transform child in caseItemGrid)
        {
            Destroy(child.gameObject);
        }

        foreach (var itemData in selectedCase.Items)
        {
            if (itemData == null)
            {
                continue;
            }

            GameObject item = Instantiate(caseItemPrefab, caseItemGrid);
            Image itemImage = item.transform.Find("ItemImage").GetComponent<Image>();
            Image rarityImage = item.transform.Find("RarityImage").GetComponent<Image>();
            TextMeshProUGUI nameText = item.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI priceText = item.transform.Find("PriceText").GetComponent<TextMeshProUGUI>();

            itemImage.sprite = Resources.Load<Sprite>($"ItemImages/{itemData.ID}");
            rarityImage.sprite = Resources.Load<Sprite>($"RarityImages/{itemData.Rarity}");
            nameText.text = itemData.Name;
            priceText.text = $"{itemData.Price:F2}";
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

    public void DisplayCaseSelector(List<CaseData> availableCases)
    {
        foreach (Transform child in caseSelectorPanel)
        {
            Destroy(child.gameObject);
        }

        List<CaseData> orderedAvailableCases = availableCases.OrderBy(i => i.Price).ToList();

        foreach (var caseData in orderedAvailableCases)
        {
            GameObject caseButton = Instantiate(caseButtonPrefab, caseSelectorPanel);
            Image caseImage = caseButton.transform.Find("CaseImage").GetComponent<Image>();
            TextMeshProUGUI nameText = caseButton.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI priceText = caseButton.transform.Find("PriceText").GetComponent<TextMeshProUGUI>();
            
            caseImage.sprite = Resources.Load<Sprite>($"CaseImages/{caseData.ID}");
            nameText.text = caseData.Name;
            priceText.text = $"{caseData.Price:F2}";

            Button button = caseButton.GetComponent<Button>();
            button.onClick.AddListener(() => SelectCase(caseData));
        }
    }
}
