using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TradeUpItem : MonoBehaviour
{
    public Image itemImage;
    public Image rarityImage;
    public Image statTrakImage;
    public TextMeshProUGUI gunText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI conditionText;
    public TextMeshProUGUI floatText;
    public TextMeshProUGUI patternText;
    public Button clickButton;

    public ItemData Item { get; private set; }

    // onClick may be null for display-only slots (e.g. the traded result).
    public void Setup(ItemData item, Action<ItemData> onClick)
    {
        Item = item;

        if (itemImage != null) itemImage.sprite = Resources.Load<Sprite>($"ItemImages/{item.imageName}");
        if (rarityImage != null) rarityImage.sprite = Resources.Load<Sprite>($"RarityImages/{item.rarity}_ROULETTE_SLIM");
        if (statTrakImage != null) statTrakImage.gameObject.SetActive(item.isStatTrak);
        if (gunText != null) gunText.text = item.gun;
        if (nameText != null) nameText.text = $"{item.name}";
        if (priceText != null) priceText.text = $"{Globals.FormatCurrencyStr(item.price, Globals.EURO_DECIMAL)}€";
        if (conditionText != null) conditionText.text = item.condition;
        if (floatText != null) floatText.text = $"{item.wearFloat}";
        if (patternText != null) patternText.text = $"{item.pattern}";

        if (clickButton != null)
        {
            clickButton.onClick.RemoveAllListeners();
            clickButton.interactable = onClick != null;
            if (onClick != null)
            {
                clickButton.onClick.AddListener(() => onClick(item));
            }
        }
    }
}
