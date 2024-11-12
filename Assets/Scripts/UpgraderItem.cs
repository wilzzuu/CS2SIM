using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgraderItem : MonoBehaviour
{
    public Image itemImage;
    public Image rarityImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public Button chooseButton;

    private ItemData _itemData;

    public void Setup(ItemData item, bool isInventoryItem)
    {
        _itemData = item;
        
        itemImage.sprite = Resources.Load<Sprite>($"ItemImages/{_itemData.ID}");
        rarityImage.sprite = Resources.Load<Sprite>($"RarityImages/{_itemData.Rarity}");
        nameText.text = _itemData.Name;
        priceText.text = $"{_itemData.Price:0.00}";

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
