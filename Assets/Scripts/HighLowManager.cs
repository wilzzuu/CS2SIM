using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HighLowManager : MonoBehaviour
{
    public Transform previousItemContainer;
    public Transform nextItemContainer;
    public GameObject highLowItemPrefab;
    public TMP_InputField betAmountInput;
    public Button guessHighButton;
    public Button guessLowButton;
    public Button nextItemButton;
    public Button startRoundButton;
    public Button cashOutButton;
    public TextMeshProUGUI cashOutText;
    public TextMeshProUGUI resultText;
    
    public UIManager uIManager;

    private float _betAmount;
    private int _roundNumber;
    private const float RoundMultiplier = 1.25f;

    private List<ItemData> _allItems = new List<ItemData>();
    private ItemData _currentItem;
    private ItemData _previousItem;

    void Start()
    {
        guessHighButton.onClick.AddListener(GuessHigh);
        guessLowButton.onClick.AddListener(GuessLow);
        nextItemButton.onClick.AddListener(NextItem);
        cashOutButton.onClick.AddListener(CashOut);
        startRoundButton.onClick.AddListener(StartRound);
        guessHighButton.interactable = false;
        guessLowButton.interactable = false;
        nextItemButton.interactable = false;
        cashOutButton.interactable = false;
        startRoundButton.interactable = true;
        betAmountInput.interactable = true;
        
        _allItems = Resources.LoadAll<ItemData>("ItemAssets").ToList();
    }

    private void SetupFirstItem()
    {
        resultText.text = "Round: 1";
        _currentItem = GetRandomItem();
        _previousItem = GetRandomItem();
        PopulatePreviousItem(_previousItem);
        PopulateNextHiddenItem();
    }
    
    private void PopulateNextHiddenItem()
    {
        foreach (Transform child in nextItemContainer)
        {
            Destroy(child.gameObject);
        }
        
        GameObject nextItem = Instantiate(highLowItemPrefab, nextItemContainer);
        Image itemImage = nextItem.transform.Find("ItemImage").GetComponent<Image>();
        Image rarityImage = nextItem.transform.Find("RarityImage").GetComponent<Image>();
        TextMeshProUGUI gunText = nextItem.transform.Find("GunText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI nameText = nextItem.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI priceText = nextItem.transform.Find("PriceText").GetComponent<TextMeshProUGUI>();
        
        itemImage.gameObject.SetActive(false);
        rarityImage.sprite = Resources.Load<Sprite>($"HiddenItemL");
        gunText.text = "????????";
        nameText.text = "??????";
        priceText.text = "?€";
    }

    private void PopulateNextRevealedItem(ItemData item)
    {
        foreach (Transform child in nextItemContainer)
        {
            Destroy(child.gameObject);
        }
        
        GameObject revealItem = Instantiate(highLowItemPrefab, nextItemContainer);
        Image itemImage = revealItem.transform.Find("ItemImage").GetComponent<Image>();
        Image rarityImage = revealItem.transform.Find("RarityImage").GetComponent<Image>();
        TextMeshProUGUI gunText = revealItem.transform.Find("GunText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI nameText = revealItem.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI priceText = revealItem.transform.Find("PriceText").GetComponent<TextMeshProUGUI>();
        
        itemImage.sprite = Resources.Load<Sprite>($"ItemImages/{item.id}");
        rarityImage.sprite = Resources.Load<Sprite>($"RarityImages/{item.rarity}");
        gunText.text = item.gun;
        nameText.text = item.name;
        priceText.text = $"{item.price:F2}€";
    }

    private void PopulatePreviousItem(ItemData item)
    {
        foreach (Transform child in previousItemContainer)
        {
            Destroy(child.gameObject);
        }
        
        GameObject prevItem = Instantiate(highLowItemPrefab, previousItemContainer);
        Image itemImage = prevItem.transform.Find("ItemImage").GetComponent<Image>();
        Image rarityImage = prevItem.transform.Find("RarityImage").GetComponent<Image>();
        TextMeshProUGUI gunText = prevItem.transform.Find("GunText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI nameText = prevItem.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI priceText = prevItem.transform.Find("PriceText").GetComponent<TextMeshProUGUI>();

        itemImage.sprite = Resources.Load<Sprite>($"ItemImages/{item.id}");
        rarityImage.sprite = Resources.Load<Sprite>($"RarityImages/{item.rarity}");
        gunText.text = item.gun;
        nameText.text = item.name;
        priceText.text = $"{item.price:F2}€";
    }
    
    private ItemData GetRandomItem()
    {
        var filteredItems = _allItems.Where(item => !Mathf.Approximately(item.price, _previousItem?.price ?? -1f)).ToList();

        if (filteredItems.Count == 0)
        {
            Debug.LogWarning("No items available with a different price than the previous item.");
            return _allItems[Random.Range(0, _allItems.Count)];
        }
        
        int randomIndex = Random.Range(0, _allItems.Count);
        return filteredItems[randomIndex];
    }

    private void StartRound()
    {
        foreach (Transform child in previousItemContainer)
        {
            Destroy(child.gameObject);
        }
        
        foreach (Transform child in nextItemContainer)
        {
            Destroy(child.gameObject);
        }
        
        if (float.TryParse(betAmountInput.text, out _betAmount) && _betAmount > 0 &&
            _betAmount <= PlayerManager.Instance.GetPlayerBalance())
        {
            PlayerManager.Instance.DeductCurrency(_betAmount);
            guessHighButton.interactable = true;
            guessLowButton.interactable = true;
            startRoundButton.interactable = false;
            betAmountInput.interactable = false;
            uIManager.LockUI();
            
            SetupFirstItem();
        }
        else
        {
            Debug.LogWarning("Invalid bet amount or not enough balance.");
        }
    }

    private void NextItem()
    {
        resultText.text = $"Round {_roundNumber+1}";
        _previousItem = _currentItem;
        _currentItem = GetRandomItem();
        guessHighButton.interactable = true;
        guessLowButton.interactable = true;
        nextItemButton.interactable = false;
        PopulateNextHiddenItem();
        PopulatePreviousItem(_previousItem);
    }

    private void GuessHigh()
    {
        guessHighButton.interactable = false;
        guessLowButton.interactable = false;
        nextItemButton.interactable = true;
        PopulateNextRevealedItem(_currentItem);
        
        if (_currentItem.price > _previousItem.price)
        {
            if (!cashOutButton.interactable) cashOutButton.interactable = true;
            _roundNumber++;
            resultText.text = $"You guessed right on round {_roundNumber}!";
            
            float cashOutAmount = _betAmount * _roundNumber * RoundMultiplier;
            cashOutText.text = $"Cash Out {cashOutAmount:F2}€ at {_roundNumber * RoundMultiplier}x";
        }
        else if (_currentItem.price < _previousItem.price)
        {
            resultText.text = "You guessed wrong and lost.";
            GameOver();
        }
        else
        {
            resultText.text = "Item prices are the same. Skipping round.";
        }
    }

    private void GuessLow()
    {
        guessHighButton.interactable = false;
        guessLowButton.interactable = false;
        nextItemButton.interactable = true;
        PopulateNextRevealedItem(_currentItem);
        
        if (_currentItem.price < _previousItem.price)
        {
            if (!cashOutButton.interactable) cashOutButton.interactable = true;
            _roundNumber++;
            resultText.text = $"You guessed right on round {_roundNumber}!";
            
            float cashOutAmount = _betAmount * _roundNumber * RoundMultiplier;
            cashOutText.text = $"Cash Out {cashOutAmount:F2}€ at {_roundNumber * RoundMultiplier}x";
        }
        else if (_currentItem.price > _previousItem.price)
        {
            resultText.text = "You guessed wrong and lost.";
            GameOver();
        }
        else
        {
            resultText.text = "Item prices are the same. Skipping round.";
        }
    }

    private void GameOver()
    {
        _roundNumber = 0;
        cashOutText.text = "";
        guessHighButton.interactable = false;
        guessLowButton.interactable = false;
        nextItemButton.interactable = false;
        cashOutButton.interactable = false;
        startRoundButton.interactable = true;
        betAmountInput.interactable = true;
        uIManager.UnlockUI();
        _previousItem = null;
        _currentItem = null;
    }
    
    private void CashOut()
    {
        float winnings = _betAmount * _roundNumber * RoundMultiplier;
        PlayerManager.Instance.AddCurrency(winnings);
        resultText.text = $"You cashed out {winnings:F2}€ at {_roundNumber * RoundMultiplier}x";
        GameOver();
    }
}
