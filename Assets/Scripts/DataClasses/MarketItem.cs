using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MarketplaceItem : MonoBehaviour
{
    public Image itemImage;
    public Image rarityImage;
    public TextMeshProUGUI conditionText;
    public TextMeshProUGUI floatText;
    public TextMeshProUGUI patternText;
    public Image statTrakImage;
    public TextMeshProUGUI gunText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI priceVariationText;
    public Button buyButton;
    public Button sellButton;

    private ItemData _itemData;

    public void Setup(ItemData item, bool isBuying)
    {
        _itemData = item;
        
        itemImage.sprite = Resources.Load<Sprite>($"ItemImages/{_itemData.imageName}");
        rarityImage.sprite = Resources.Load<Sprite>($"RarityImages/{_itemData.rarity}_MARKET");
        conditionText.text = _itemData.condition;
        if (_itemData.isStatTrak)
        {
            statTrakImage.gameObject.SetActive(true);
        }
        else
        {
            statTrakImage.gameObject.SetActive(false);
        }

        gunText.text = _itemData.gun;
        nameText.text = _itemData.name;
        floatText.gameObject.SetActive(!isBuying);
        patternText.gameObject.SetActive(!isBuying);
        floatText.text = $"{_itemData.wearFloat}";
        patternText.text = $"{_itemData.pattern}";
        UpdatePrice(isBuying);

        buyButton.gameObject.SetActive(isBuying);
        sellButton.gameObject.SetActive(!isBuying);

        if (isBuying)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => MarketManager.Instance.BuyItem(_itemData));
        }
        else
        {
            sellButton.onClick.RemoveAllListeners();
            sellButton.onClick.AddListener(() => MarketManager.Instance.SellItem(_itemData, gameObject));
        }
    }
    
    public void UpdatePrice(bool isBuying)
    {
        int buyPrice = Globals.GetMarketBuyPrice(_itemData);
        priceText.text = isBuying
            ? $"{Globals.FormatCurrencyStr(buyPrice, Globals.EURO_DECIMAL)}€"
            : $"{Globals.FormatCurrencyStr(_itemData.price, Globals.EURO_DECIMAL)}€";
        
        int priceVariation = isBuying
            ? buyPrice - _itemData.ModifiedBasePrice
            : _itemData.price - _itemData.ModifiedBasePrice;
        priceVariationText.text = priceVariation > 0
            ? $"+{Globals.FormatCurrencyStr(priceVariation, Globals.EURO_DECIMAL)}€"
            : $"{Globals.FormatCurrencyStr(priceVariation, Globals.EURO_DECIMAL)}€";
        priceVariationText.color = priceVariation > 0
            ? new Color32(215, 49, 49, 255)
            : new Color32(104, 215, 49, 255);
    }
}
