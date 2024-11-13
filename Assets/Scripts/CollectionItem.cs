using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CollectionItem : MonoBehaviour
{
    private static readonly int Color1 = Shader.PropertyToID("_Color");
    public Image itemImage;
    public Image rarityImage;
    public TextMeshProUGUI itemNameText;

    public Material grayscaleMaterial;
    private Material _originalMaterial;

    public void Setup(ItemData item, bool isCollected)
    {
        itemImage.sprite = Resources.Load<Sprite>($"ItemImages/{item.id}");
        rarityImage.sprite = Resources.Load<Sprite>($"RarityImages/{item.rarity}");
        itemNameText.text = item.name;

        if (!_originalMaterial)
        {
            _originalMaterial = itemImage.material;
        }

        if (!isCollected)
        {
            itemImage.material = grayscaleMaterial;
            itemImage.material.SetColor(Color1, Color.gray);
        }
        else
        {
            itemImage.material = _originalMaterial;
        }
    }
}
