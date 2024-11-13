using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class InventoryDebugger : EditorWindow
    {
        private List<ItemData> _itemAssets; // List to store all item assets
        private ItemData _selectedItem;     // The selected item to add to inventory

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

            if (_itemAssets == null || _itemAssets.Count == 0)
            {
                GUILayout.Label("No items found in ItemAssets folder.");
                if (GUILayout.Button("Reload Items"))
                {
                    LoadAllItemAssets();
                }
                return;
            }

            _selectedItem = (ItemData)EditorGUILayout.ObjectField("Select Item", _selectedItem, typeof(ItemData), false);

            if (_selectedItem && GUILayout.Button("Add Selected Item to Inventory"))
            {
                AddItemToInventory(_selectedItem);
            }
        }

        // Load all ItemData assets from the specified folder
        // ReSharper disable Unity.PerformanceAnalysis
        private void LoadAllItemAssets()
        {
            _itemAssets = new List<ItemData>(Resources.LoadAll<ItemData>("Items"));
            Debug.Log($"Loaded {_itemAssets.Count} item assets from ItemAssets folder.");
        }

        // Adds the selected item to the inventory
        // ReSharper disable Unity.PerformanceAnalysis
        private void AddItemToInventory(ItemData item)
        {
            if (InventoryManager.Instance)
            {
                InventoryManager.Instance.AddItemToInventory(item);
                Debug.Log($"Added {item.name} to inventory.");
            }
            else
            {
                Debug.LogWarning("InventoryManager instance not found in the scene.");
            }
        }
    }
}
