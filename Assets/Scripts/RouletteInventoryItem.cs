using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RouletteInventoryItem : MonoBehaviour
{
    public Image itemImage;
    public Image rarityImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public Button chooseButton;
    public TextMeshProUGUI buttonText;

    public void Setup(ItemData item, bool isItemSelected)
    {
        itemImage.sprite = Resources.Load<Sprite>($"ItemImages/{item.ID}");
        rarityImage.sprite = Resources.Load<Sprite>($"RarityImages/{item.Rarity}");
        nameText.text = item.Name;
        priceText.text = $"{item.Price:0.00}";

        if (!isItemSelected)
        {
            chooseButton.onClick.RemoveAllListeners();
            chooseButton.onClick.AddListener(() => RouletteManager.Instance.AddItemToSelection(item));
            buttonText.text = "Select";
        }
        else
        {
            chooseButton.onClick.RemoveAllListeners();
            chooseButton.onClick.AddListener(() => RouletteManager.Instance.RemoveItemFromSelection(item));
            buttonText.text = "Unselect";
        }
    }
}
