using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MarketplaceItem : MonoBehaviour
{
    public Image itemImage;
    public Image rarityImage;
    public TextMeshProUGUI conditionText;
    public TextMeshProUGUI statTrakText;
    public TextMeshProUGUI gunText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI priceVariationText;
    public Button buyButton;
    public Button sellButton;

    private ItemData _itemData;

    private const float BuyMarkup = 1.15f;

    public void Setup(ItemData item, bool isBuying)
    {
        _itemData = item;
        
        itemImage.sprite = Resources.Load<Sprite>($"ItemImages/{_itemData.imageName}");
        rarityImage.sprite = Resources.Load<Sprite>($"RarityImages/{_itemData.rarity}");
        conditionText.text = _itemData.condition;
        if (_itemData.isStatTrak)
        {
            statTrakText.text = Globals.StatTrakUIText;
            statTrakText.gameObject.SetActive(true);
        }
        else
        {
            statTrakText.text = null;
            statTrakText.gameObject.SetActive(false);
        }

        gunText.text = _itemData.gun;
        nameText.text = _itemData.name;
        priceText.text = isBuying
            ? $"{_itemData.price * BuyMarkup:F2}€"
            : $"{_itemData.price:F2}€";
        
        float priceVariation = isBuying
            ? _itemData.price * BuyMarkup - _itemData.basePrice
            : _itemData.price - _itemData.basePrice;
        priceVariationText.text = priceVariation > 0
            ? $"+{priceVariation:F2}€"
            : $"{priceVariation:F2}€";
        priceVariationText.color = priceVariation > 0
            ? new Color32(215, 49, 49, 255)
            : new Color32(104, 215, 49, 255);

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
        InventoryManager.Instance.CalculateInventoryValue();
        priceText.text = isBuying
            ? $"{_itemData.price * BuyMarkup:F2}€"
            : $"{_itemData.price:F2}€";
        
        float priceVariation = isBuying
            ? _itemData.price * BuyMarkup - _itemData.basePrice
            : _itemData.price - _itemData.basePrice;
        priceVariationText.text = priceVariation > 0
            ? $"+{priceVariation:F2}€"
            : $"{priceVariation:F2}€";
        priceVariationText.color = priceVariation > 0
            ? new Color32(104, 215, 49, 255)
            : new Color32(215, 49, 49, 255);
    }
}
