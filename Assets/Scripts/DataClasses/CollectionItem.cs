using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CollectionItem : MonoBehaviour
{
    private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

    public Image itemImage;
    public Image rarityImage;
    public TextMeshProUGUI gunText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI conditionText;
    public TextMeshProUGUI statTrakText;

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
        nameText.text = item.name;
        if (gunText != null) gunText.text = item.gun;
        if (conditionText != null) conditionText.text = item.condition;
        if (statTrakText != null) statTrakText.gameObject.SetActive(item.isStatTrak);

        if (!_initialized)
        {
            _originalItemMaterial = itemImage.material;
            _originalRarityMaterial = rarityImage.material;
            _originalItemColor = itemImage.color;
            _originalRarityColor = rarityImage.color;
            _originalNameColor = nameText.color;
            if (gunText != null) _originalGunColor = gunText.color;
            if (conditionText != null) _originalConditionColor = conditionText.color;
            if (statTrakText != null) _originalStatTrakColor = statTrakText.color;
            _initialized = true;
        }

        itemImage.material = !isCollected && grayscaleMaterial != null ? grayscaleMaterial : _originalItemMaterial;
        rarityImage.material = !isCollected && grayscaleMaterial != null ? grayscaleMaterial : _originalRarityMaterial;
        itemImage.color = isCollected ? _originalItemColor : Color.gray;
        rarityImage.color = isCollected ? _originalRarityColor : Color.gray;
        nameText.color = isCollected ? _originalNameColor : Color.gray;
        if (gunText != null) gunText.color = isCollected ? _originalGunColor : Color.gray;
        if (conditionText != null) conditionText.color = isCollected ? _originalConditionColor : Color.gray;
        if (statTrakText != null) statTrakText.color = isCollected ? _originalStatTrakColor : Color.gray;
    }

    private static Sprite LoadSprite(string path)
    {
        if (!SpriteCache.TryGetValue(path, out Sprite sprite))
        {
            sprite = Resources.Load<Sprite>(path);
            SpriteCache.Add(path, sprite);
        }
        return sprite;
    }
}
