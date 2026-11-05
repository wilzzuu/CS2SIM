using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class CustomCaseToScriptableObject : EditorWindow
    {
        private string _jsonPath = "";

        [MenuItem("Tools/Import Custom Case Data")]
        public static void ShowWindow()
        {
            GetWindow<CustomCaseToScriptableObject>("Import Custom Case Data");
        }
        
        // ReSharper disable Unity.PerformanceAnalysis
        void OnGUI()
        {
            GUILayout.Label("JSON Data", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            _jsonPath = EditorGUILayout.TextField("JSON File", _jsonPath);

            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                string selectedPath = EditorUtility.OpenFilePanel(
                    "Select JSON File",
                    Application.dataPath + "/StreamingAssets/CustomCases",
                    "json"
                );

                if (!string.IsNullOrEmpty(selectedPath))
                {
                    _jsonPath = selectedPath;
                }
            }

            EditorGUILayout.EndHorizontal();

            GUI.enabled = File.Exists(_jsonPath);
            
            if (GUILayout.Button("Import JSON Data"))
            {
                ImportCustomJsonData(_jsonPath);
            }

            GUI.enabled = true;

            if (!string.IsNullOrEmpty(_jsonPath))
            {
                EditorGUILayout.HelpBox(
                    $"Selected: {Path.GetFileName(_jsonPath)}",
                    MessageType.Info
                );
            }
        }
        
        // ReSharper disable Unity.PerformanceAnalysis
        public void ImportCustomJsonData(string path)
        {

            string jsonContent = File.ReadAllText(path);
            Debug.Log("JSON Content: " + jsonContent);

            try
            {
                // Parse JSON into JsonDataWrapper
                JsonDataWrapper dataWrapper = JsonUtility.FromJson<JsonDataWrapper>(jsonContent);

                // Validate parsed data
                if (dataWrapper?.cases == null)
                {
                    Debug.LogError("Failed to parse JSON: Case data is null or empty.");
                    return;
                }

                foreach (var caseData in dataWrapper.cases)
                {
                    Debug.Log($"Processing case: {caseData.ID} | {caseData.NAME} | Price: {caseData.PRICE}");
                    CreateCustomCaseDataAsset(caseData);
                }

                Debug.Log("Data imported successfully!");
            }
            catch (Exception ex)
            {
                Debug.LogError("Error parsing JSON: " + ex.Message);
            }
        }

        private static void CreateCustomCaseDataAsset(CaseJsonData jsonData)
        {
            // Create CaseData ScriptableObject
            CaseData caseAsset = CreateInstance<CaseData>();
            caseAsset.id = jsonData.ID;
            caseAsset.name = jsonData.NAME;
            caseAsset.price = JsonPriceConversion.ToCents(jsonData.PRICE);
            
            caseAsset.items = new List<ItemData>();
            
            // Create ItemData assets and add them to the case
            foreach (var itemJson in jsonData.ITEMS)
            {   
                
                if (itemJson == null)
                {
                    Debug.LogWarning("Item data is missing or null.");
                    continue;
                }
                
                // Create ItemData ScriptableObject
                foreach (var cond in itemJson.CONDITIONS)
                {   
                    string itemID = $"{itemJson.ID}-{AbbreviatedConditions.CondList[cond.condition]}";
                    
                    ItemData itemAsset = FindItemAsset(itemID);

                    if (itemAsset != null)
                    {
                        caseAsset.items.Add(itemAsset);

                        Debug.Log($"Linked item: {itemAsset.id}");
                    }
                    else
                    {
                        Debug.LogWarning($"Item not Found: {itemID}");
                    }

                    if (itemJson.HAS_ST)
                    {
                        string itemSTID = $"{itemJson.ID}-{AbbreviatedConditions.CondList[cond.condition]}st";

                        ItemData itemSTAsset = FindItemAsset(itemSTID);

                        if (itemSTAsset != null)
                        {
                            caseAsset.items.Add(itemSTAsset);

                            Debug.Log($"Linked item: {itemSTAsset.id}");
                        }
                        else
                        {
                            Debug.LogWarning($"Item not Found: {itemSTID}");
                        }
                    }
                }
            }

            SaveCustomAsset(caseAsset, $"Assets/Resources/CustomCaseAssets/{caseAsset.id}.asset");
        }

        private static ItemData FindItemAsset(string itemID)
        {
            string[] guids = AssetDatabase.FindAssets(
                $"t:ItemData {itemID}",
                new[] { "Assets/Resources/ItemAssets" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);

                if (item != null && item.id == itemID)
                {
                    return item;
                }
            }

            return null;
        }

        private static void SaveCustomAsset(ScriptableObject asset, string path)
        {
            Debug.Log(path);
            // Ensure the directory exists
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                if (directory != null) Directory.CreateDirectory(directory);
            }

            // Create and save the asset
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
        }
    }
}