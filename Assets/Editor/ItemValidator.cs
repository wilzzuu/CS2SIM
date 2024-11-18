using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class ItemValidator : EditorWindow
    {
        private List<CaseData> _caseAssets;
        private List<ItemData> _itemAssets;
        private CaseData _selectedCase;
        private string _caseID;

        [MenuItem("Tools/Item Validator")]
        public static void ShowWindow()
        {
            GetWindow<ItemValidator>("Item Validator");
        }
        
        private void OnEnable()
        {
            LoadAllCaseAssets();
            LoadAllItemAssets();
        }
        
        void OnGUI()
        {
            GUILayout.Label("Validate case item rarity", EditorStyles.boldLabel);
            
            if (_caseAssets == null || _caseAssets.Count == 0)
            {
                GUILayout.Label("No item assets selected");
                if (GUILayout.Button("Reload Cases"))
                {
                    LoadAllCaseAssets();
                }

                if (GUILayout.Button("Reload Items"))
                {
                    LoadAllItemAssets();
                }
                return;
            }
            
            _selectedCase = (CaseData)EditorGUILayout.ObjectField("Select Item", _selectedCase, typeof(CaseData), false);

            if (_selectedCase && GUILayout.Button("Validate"))
            {
                ValidateCase(_selectedCase);
            }
        }
        
        // ReSharper disable Unity.PerformanceAnalysis
        private void LoadAllCaseAssets()
        {
            _caseAssets = new List<CaseData>(Resources.LoadAll<CaseData>("CaseAssets"));
            Debug.Log($"Loaded {_caseAssets.Count} case assets from CaseAssets folder.");
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private void LoadAllItemAssets()
        {
            _itemAssets = new List<ItemData>(Resources.LoadAll<ItemData>("ItemAssets"));
            Debug.Log($"Loaded {_itemAssets.Count} item assets from ItemAssets folder.");
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private void ValidateCase(CaseData caseData)
        {
            foreach (var item in caseData.items)
            {
                string itemRarity = item.rarity;
                if (!RarityOrder.RarityOrderList.ContainsKey(itemRarity))
                {
                    Debug.Log($"Item {item.id} has an invalid Rarity of {itemRarity}");
                }
                
                if (!_itemAssets.Contains(item))
                {
                    Debug.Log($"Item {item.id} is an invalid case item");
                }
            }
        }
    }
}