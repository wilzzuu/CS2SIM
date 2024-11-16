using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class JsonToScriptableObject : EditorWindow
    {
        private string _caseID;

        [MenuItem("Tools/Import JSON Data")]
        public static void ShowWindow()
        {
            GetWindow<JsonToScriptableObject>("Import JSON Data");
        }
        
        // ReSharper disable Unity.PerformanceAnalysis
        void OnGUI()
        {
            GUILayout.Label("JSON Data", EditorStyles.boldLabel);
            
            _caseID = EditorGUILayout.TextField("Case ID", _caseID);
            string path = Application.dataPath + $"/StreamingAssets/CaseDataJSON/{_caseID}.json";

            if (File.Exists(path) && GUILayout.Button("Import JSON Data"))
            {
                ImportJsonData(_caseID);
            }
        }
        
        // ReSharper disable Unity.PerformanceAnalysis
        public void ImportJsonData(string caseID)
        {
            // Define the path to your JSON file in StreamingAssets
            string path = Application.dataPath + $"/StreamingAssets/CaseDataJSON/{caseID}.json";

            if (File.Exists(path))
            {
                string jsonContent = File.ReadAllText(path);
                Debug.Log("JSON Content: " + jsonContent);

                try
                {
                    // Parse JSON into CaseJsonDataWrapper
                    CaseJsonDataWrapper caseDataWrapper = JsonUtility.FromJson<CaseJsonDataWrapper>(jsonContent);

                    // Validate parsed data
                    if (caseDataWrapper?.cases == null)
                    {
                        Debug.LogError("Failed to parse JSON: Case data is null or empty.");
                        return;
                    }

                    foreach (var caseData in caseDataWrapper.cases)
                    {
                        Debug.Log($"Processing case: {caseData.ID} | Name: {caseData.NAME} | Price: {caseData.PRICE}");
                        CreateCaseDataAsset(caseData);
                    }

                    Debug.Log("Data imported successfully!");
                }
                catch (Exception ex)
                {
                    Debug.LogError("Error parsing JSON: " + ex.Message);
                }
            }
            else
            {
                Debug.LogError("JSON file not found at path: " + path);
            }
        }

        private static void CreateCaseDataAsset(CaseJsonData jsonData)
        {
            // Create CaseData ScriptableObject
            CaseData caseAsset = ScriptableObject.CreateInstance<CaseData>();
            caseAsset.id = jsonData.ID;
            caseAsset.name = jsonData.NAME;
            caseAsset.price = jsonData.PRICE;
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
                ItemData itemAsset = ScriptableObject.CreateInstance<ItemData>();

                bool statTrak = itemJson.ID.EndsWith("ST");
                string condition = SplitIDString(itemJson.ID);
                
                // Log each item for debugging
                Debug.Log($"Creating item: {condition} {itemJson.GUN} {itemJson.NAME} | Rarity: {itemJson.RARITY} | Price: {itemJson.PRICE} | ID: {itemJson.ID}");
                
                itemAsset.id = itemJson.ID;
                itemAsset.gun = itemJson.GUN;
                itemAsset.name = itemJson.NAME;
                itemAsset.basePrice = itemJson.PRICE;
                itemAsset.price = itemJson.PRICE;
                itemAsset.condition = condition;
                itemAsset.isStatTrak = statTrak;
                itemAsset.type = itemJson.TYPE;
                itemAsset.rarity = itemJson.RARITY;
                itemAsset.weight = itemJson.WEIGHT;

                // Save each ItemData asset and add it to the CaseData
                SaveAsset(itemAsset, $"Assets/Resources/ItemAssets/{itemAsset.id}.asset");
                caseAsset.items.Add(itemAsset);
            }

            // Save CaseData asset
            SaveAsset(caseAsset, $"Assets/Resources/CaseAssets/{jsonData.ID}.asset");
        }

        private static void SaveAsset(ScriptableObject asset, string path)
        {
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

        private static string SplitIDString(string id)
        {
            string idSplit = id.Split('_')[1];
            if (idSplit.EndsWith("ST")) idSplit = idSplit.Split("ST")[0];
            
            string pattern = @"\d+(.*)";
            Match match = Regex.Match(idSplit, pattern);

            if (match.Success)
            {
                string shortCondition = match.Groups[1].Value;
                string condition = ConvertConditionToLongForm(shortCondition);
                
                return condition;
            }
            return "Unknown";
        }
        
        private static string ConvertConditionToLongForm(string condition)
        {
            switch (condition)
            {
                case "FN": return "Factory New";
                case "MW": return "Minimal Wear";
                case "FT": return "Field-Tested";
                case "WW": return "Well-Worn";
                case "BS": return "Battle-Scarred";
                case "" : return "Vanilla";
                default: return "Unknown";
            }
        }
    }

    // Wrapper class for the cases list
    [Serializable]
    public class CaseJsonDataWrapper
    {
        public List<CaseJsonData> cases;
    }

    // Structure of Case JSON data
    [Serializable]
    public class CaseJsonData
    {
        public string ID;
        public string NAME;
        public float PRICE;
        public List<ItemJsonData> ITEMS;
    }

    // Structure of Item JSON data
    [Serializable]
    public class ItemJsonData
    {
        public string ID;
        public string GUN;
        public string NAME;
        public float PRICE;
        public string TYPE;
        public string RARITY;
        public float WEIGHT;
    }
}