using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class InventoryDebugger : EditorWindow
{
    private List<ItemData> itemAssets; // List to store all item assets
    private ItemData selectedItem;     // The selected item to add to inventory

    [MenuItem("Tools/Inventory Debugger")]
    public static void ShowWindow()
    {
        GetWindow<InventoryDebugger>("Inventory Debugger");
    }

    private void OnEnable()
    {
        LoadAllItemAssets();
    }

    void OnGUI()
    {
        GUILayout.Label("Add Items to Inventory", EditorStyles.boldLabel);

        if (itemAssets == null || itemAssets.Count == 0)
        {
            GUILayout.Label("No items found in ItemAssets folder.");
            if (GUILayout.Button("Reload Items"))
            {
                LoadAllItemAssets();
            }
            return;
        }

        selectedItem = (ItemData)EditorGUILayout.ObjectField("Select Item", selectedItem, typeof(ItemData), false);

        if (selectedItem != null && GUILayout.Button("Add Selected Item to Inventory"))
        {
            AddItemToInventory(selectedItem);
        }
    }

    // Load all ItemData assets from the specified folder
    private void LoadAllItemAssets()
    {
        itemAssets = new List<ItemData>(Resources.LoadAll<ItemData>("Items"));
        Debug.Log($"Loaded {itemAssets.Count} item assets from ItemAssets folder.");
    }

    // Adds the selected item to the inventory
    private void AddItemToInventory(ItemData item)
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItemToInventory(item);
            Debug.Log($"Added {item.Name} to inventory.");
        }
        else
        {
            Debug.LogWarning("InventoryManager instance not found in the scene.");
        }
    }
}
