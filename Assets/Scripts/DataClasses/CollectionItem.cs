using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CollectionItem : MonoBehaviour
{
    public Image itemImage;
    public Image rarityImage;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI gunText;
    public TextMeshProUGUI conditionText;
    public Image statTrakIndicator;

    public Material grayscaleMaterial;
    private Material _originalItemMaterial;
    private Material _originalRarityMaterial;
    private Color _originalItemColor;
    private Color _originalRarityColor;
    private Color _originalNameColor;
    private Color _originalGunColor;
    private Color _originalConditionColor;
    private Color _originalStatTrakColor;
    private bool _initialized;

    public void Setup(ItemData item, bool isCollected, Sprite itemSprite, Sprite raritySprite)
    {
        itemImage.sprite = itemSprite;
        rarityImage.sprite = raritySprite;
        itemNameText.text = item.name;
        if (gunText != null) gunText.text = item.gun;
        if (conditionText != null) conditionText.text = item.condition;
        if (statTrakIndicator != null) statTrakIndicator.gameObject.SetActive(item.isStatTrak);

        if (!_initialized)
        {
            _originalItemMaterial = itemImage.material;
            _originalRarityMaterial = rarityImage.material;
            _originalItemColor = itemImage.color;
            _originalRarityColor = rarityImage.color;
            _originalNameColor = itemNameText.color;
            if (gunText != null) _originalGunColor = gunText.color;
            if (conditionText != null) _originalConditionColor = conditionText.color;
            if (statTrakIndicator != null) _originalStatTrakColor = statTrakIndicator.color;
            _initialized = true;
        }

        itemImage.material = !isCollected && grayscaleMaterial != null ? grayscaleMaterial : _originalItemMaterial;
        rarityImage.material = !isCollected && grayscaleMaterial != null ? grayscaleMaterial : _originalRarityMaterial;
        itemImage.color = isCollected ? _originalItemColor : Color.gray;
        rarityImage.color = isCollected ? _originalRarityColor : Color.gray;
        itemNameText.color = isCollected ? _originalNameColor : Color.gray;
        if (gunText != null) gunText.color = isCollected ? _originalGunColor : Color.gray;
        if (conditionText != null) conditionText.color = isCollected ? _originalConditionColor : Color.gray;
        if (statTrakIndicator != null) statTrakIndicator.color = isCollected ? _originalStatTrakColor : Color.gray;
    }
}
