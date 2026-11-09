using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RouletteInventoryItem : MonoBehaviour
{
    public Image itemImage;
    public Image rarityImage;
    public Image statTrakImage;
    public TextMeshProUGUI gunText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI conditionText;
    public Button chooseButton;
    public TextMeshProUGUI buttonText;

    public void Setup(ItemData item, bool isItemSelected, int inventoryIndex)
    {
        itemImage.sprite = Resources.Load<Sprite>($"ItemImages/{item.imageName}");
        rarityImage.sprite = Resources.Load<Sprite>($"RarityImages/{item.rarity}_ROULETTE_SLIM");

        if (item.isStatTrak)
        {
            statTrakImage.enabled = true;
        }
        else
        {
            statTrakImage.enabled = false;
        }

        gunText.text = item.gun;
        nameText.text = item.name;
        priceText.text = $"{Globals.FormatCurrencyStr(item.price, Globals.EURO_DECIMAL)}€";
        conditionText.text = item.condition;

        if (!isItemSelected)
        {
            chooseButton.onClick.RemoveAllListeners();
            chooseButton.onClick.AddListener(() => RouletteManager.Instance.AddItemToSelection(inventoryIndex));
            buttonText.text = "Select";
        }
        else
        {
            chooseButton.onClick.RemoveAllListeners();
            chooseButton.onClick.AddListener(() => RouletteManager.Instance.RemoveItemFromSelection(inventoryIndex));
            buttonText.text = "Unselect";
        }
    }
}
