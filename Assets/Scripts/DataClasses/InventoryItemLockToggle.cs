using UnityEngine;

public class InventoryItemLockToggle : MonoBehaviour
{
    public GameObject lockedIcon;
    public GameObject unlockedIcon;

    private ItemData _item;

    public void Setup(ItemData item)
    {
        _item = item;
        UpdateLockIcon();
    }

    public void ToggleLock()
    {
        if (_item == null)
        {
            Debug.LogError("Cannot toggle the lock state before an inventory item is assigned.");
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager instance is not set.");
            return;
        }

        if (InventoryManager.Instance.SetItemLocked(_item, !_item.isLocked))
        {
            UpdateLockIcon();
        }
    }

    private void UpdateLockIcon()
    {
        if (_item == null)
        {
            return;
        }

        if (lockedIcon != null) lockedIcon.SetActive(_item.isLocked);
        if (unlockedIcon != null) unlockedIcon.SetActive(!_item.isLocked);
    }
}
