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

        [MenuItem("Tools/Import JSON Data")]
        public static void ShowWindow()
        {
            GetWindow<JsonToScriptableObject>("Import JSON Data");
        }
        
        // ReSharper disable Unity.PerformanceAnalysis
        void OnGUI()
        {
            GUILayout.Label("JSON Data", EditorStyles.boldLabel);
            
            string path = Application.dataPath + $"/StreamingAssets/data.json";

            if (File.Exists(path) && GUILayout.Button("Import JSON Data"))
            {
                ImportJsonData(path);
            }
        }
        
        // ReSharper disable Unity.PerformanceAnalysis
        public void ImportJsonData(string path)
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
                    CreateCaseDataAsset(caseData);
                }

                Debug.Log("Data imported successfully!");
            }
            catch (Exception ex)
            {
                Debug.LogError("Error parsing JSON: " + ex.Message);
            }
        }

        private static void CreateCaseDataAsset(CaseJsonData jsonData)
        {
            // Create CaseData ScriptableObject
            CaseData caseAsset = CreateInstance<CaseData>();
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
                foreach (var cond in itemJson.CONDITIONS)
                {   
                    ItemData itemAsset = CreateInstance<ItemData>();
                    string itemID = $"{itemJson.ID}_{AbbreviatedConditions.CondList[cond.condition]}";
                    itemAsset.id = itemID;
                    itemAsset.gun = itemJson.GUN;
                    itemAsset.name = itemJson.NAME;
                    itemAsset.basePrice = cond.regular_price;
                    itemAsset.price = cond.regular_price;
                    itemAsset.condition = cond.condition;
                    itemAsset.type = itemJson.TYPE;
                    itemAsset.rarity = itemJson.RARITY;
                    itemAsset.weight = RarityWeights.WeightList[itemJson.RARITY];
                    Debug.Log($"Creating item: {cond.condition} {itemJson.GUN} {itemJson.NAME} | ID: {itemID}");
                    
                    SaveAsset(itemAsset, $"Assets/Resources/ItemAssets/{itemID}.asset");
                    caseAsset.items.Add(itemAsset);

                    if (itemJson.HAS_ST == true)
                    {
                        ItemData itemAssetST = CreateInstance<ItemData>();
                        string itemSTID = $"{itemJson.ID}_{AbbreviatedConditions.CondList[cond.condition]}ST";
                        itemAssetST.id = itemSTID;
                        itemAssetST.gun = itemJson.GUN;
                        itemAssetST.name = itemJson.NAME;
                        itemAssetST.basePrice = cond.stattrak_price;
                        itemAssetST.price = cond.stattrak_price;
                        itemAssetST.condition = cond.condition;
                        itemAssetST.type = itemJson.TYPE;
                        itemAssetST.isStatTrak = itemJson.HAS_ST;
                        itemAssetST.rarity = itemJson.RARITY;
                        itemAssetST.weight = RarityWeights.WeightList[itemJson.RARITY];

                        Debug.Log($"Creating item: ST {cond.condition} {itemJson.GUN} {itemJson.NAME} | ID: {itemSTID}");

                        SaveAsset(itemAssetST, $"Assets/Resources/ItemAssets/{itemSTID}.asset");
                        caseAsset.items.Add(itemAssetST);
                    }
                }
            }

            SaveAsset(caseAsset, $"Assets/Resources/CaseAssets/{caseAsset.id}.asset");
        }

        private static void SaveAsset(ScriptableObject asset, string path)
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

    [Serializable]
    public class AbbreviatedConditions
    {
        public static readonly Dictionary<string, string> CondList = new Dictionary<string, string>
        {
            {"Factory New", "FN"},
            {"Minimal Wear", "MW"},
            {"Field-Tested", "FT"},
            {"Well-Worn", "WW"},
            {"Battle-Scarred", "BS"},
            {"Vanilla", ""}
        };
    }

    [Serializable]
    public class RarityWeights
    {
        public static readonly Dictionary<string, float> WeightList = new Dictionary<string, float>
        {
            {"Mil-Spec", 0.7992f},
            {"Restricted", 0.1598f},
            {"Classified", 0.032f},
            {"Covert", 0.0064f},
            {"Special", 0.0026f}
        };
    }

    // Wrapper class for the cases list
    [Serializable]
    public class JsonDataWrapper
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
        public string RARITY;
        public string TYPE;
        public bool HAS_ST;
        public List<ConditionData> CONDITIONS;
    }

    [Serializable]
    public class ConditionData
    {
        public string condition;
        public float regular_price;
        public float stattrak_price;
    }
}