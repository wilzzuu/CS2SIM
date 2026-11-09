using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgraderItem : MonoBehaviour
{
    public Image itemImage;
    public Image rarityImage;
    public Image statTrakImage;
    public TextMeshProUGUI gunText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI conditionText;
    public Button chooseButton;

    private ItemData _itemData;

    public void Setup(ItemData item, bool isInventoryItem)
    {
        _itemData = item;
        
        itemImage.sprite = Resources.Load<Sprite>($"ItemImages/{_itemData.imageName}");
        rarityImage.sprite = Resources.Load<Sprite>($"RarityImages/{_itemData.rarity}_ROULETTE_SLIM");
        
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
        priceText.text = $"{Globals.FormatCurrencyStr(_itemData.price, Globals.EURO_DECIMAL)}€";
        conditionText.text = _itemData.condition;

        if (isInventoryItem)
        {
            chooseButton.onClick.RemoveAllListeners();
            chooseButton.onClick.AddListener(() => UpgraderManager.Instance.SelectInventoryItem(_itemData));
        }
        else
        {
            chooseButton.onClick.RemoveAllListeners();
            chooseButton.onClick.AddListener(() => UpgraderManager.Instance.SelectUpgradeItem(_itemData));
        }
    }
}
