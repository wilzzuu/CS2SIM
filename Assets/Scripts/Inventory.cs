using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryScreen : MonoBehaviour
{
    public GameObject inventorySlotPrefab;
    public Transform inventoryGrid;
    public Text totalValueText;
    public int itemsPerPage = 112;
    private int _currentPage;

    private List<ItemData> _items = new List<ItemData>();

    void Start()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager instance is not set.");
            return;
        }

        _items = InventoryManager.Instance.GetInventoryItems();
        if (_items == null || _items.Count == 0)
        {
            Debug.LogWarning("No items found in inventory.");
            return;
        }

        DisplayCurrentPage();
        DisplayTotalValue();
    }

    void DisplayCurrentPage()
    {
        foreach (Transform child in inventoryGrid)
        {
            Destroy(child.gameObject);
        }

        int itemIndexOffset = _currentPage * itemsPerPage;
        int endIndex = Mathf.Min(itemIndexOffset + itemsPerPage, _items.Count);

        for (int i = itemIndexOffset; i < endIndex; i++)
        {
            GameObject slot = Instantiate(inventorySlotPrefab, inventoryGrid);
            ItemData item = _items[i];

            Image itemImage = slot.transform.Find("ItemImage")?.GetComponent<Image>();
            Image rarityImage = slot.transform.Find("RarityImage")?.GetComponent<Image>();

            if (itemImage != null)
            {
                string imagePath = "ItemImages/" + item.ID;
                Sprite itemSprite = Resources.Load<Sprite>(imagePath);
                if (itemSprite != null)
                {
                    itemImage.sprite = itemSprite;
                }
                else
                {
                    Debug.LogWarning($"Failed to load item image for path: {imagePath}");
                }
            }

            if (rarityImage != null)
            {
                string rarityPath = "RarityImages/" + item.Rarity;
                Sprite raritySprite = Resources.Load<Sprite>(rarityPath);
                if (raritySprite != null)
                {
                    rarityImage.sprite = raritySprite;
                }
                else
                {
                    Debug.LogWarning($"Failed to load rarity image for path: {rarityPath}");
                }
            }
        }
    }

    public void NextPage()
    {
        if ((_currentPage + 1) * itemsPerPage < _items.Count)
        {
            _currentPage++;
            DisplayCurrentPage();
        }
    }

    public void PreviousPage()
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            DisplayCurrentPage();
        }
    }

    public void DisplayTotalValue()
    {
        float totalValue = InventoryManager.Instance.CalculateInventoryValue();

        if (totalValueText != null)
        {
            totalValueText.text = $"Total Value: {totalValue:F2}";
        }
    }
}
