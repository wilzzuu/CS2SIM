using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class JsonToScriptableObject : EditorWindow
    {
        private string _jsonPath = "";

        [MenuItem("Tools/Import JSON Data")]
        public static void ShowWindow()
        {
            GetWindow<JsonToScriptableObject>("Import JSON Data");
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
                    Application.dataPath + "/StreamingAssets",
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
                ImportJsonData(_jsonPath);
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
        public void ImportJsonData(string path)
        {

            string caseAssetPath = Application.dataPath + "/Resources/CaseAssets/";

            foreach (string file in Directory.GetFiles(caseAssetPath))
            {
                File.Delete(file);
            }
            Debug.Log("Old Case Assets Deleted");

            string itemAssetPath = Application.dataPath + "/Resources/ItemAssets/";

            foreach (string file in Directory.GetFiles(itemAssetPath))
            {
                File.Delete(file);
            }
            Debug.Log("Old Item Assets Deleted");

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
                
                int casesComplete = 0;
                int caseCount = dataWrapper.cases.Count;
                foreach (var caseData in dataWrapper.cases)
                {
                    Debug.Log($"Processing case: {caseData.ID} | {caseData.NAME} | Price: {caseData.PRICE}");
                    CreateCaseDataAsset(caseData);
                    casesComplete++;
                    Debug.Log($"Cases Done: {casesComplete}/{caseCount}");
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
                    string itemPath = $"Assets/Resources/ItemAssets/{itemID}.asset";
                    ItemData itemAsset = GetOrCreateItemAsset(itemID, itemPath);
                    int regularPrice = JsonPriceConversion.ToCents(cond.regular_price);
                    itemAsset.id = itemID;
                    itemAsset.gun = itemJson.GUN;
                    itemAsset.name = itemJson.NAME;
                    itemAsset.basePrice = regularPrice;
                    itemAsset.price = regularPrice;
                    itemAsset.condition = cond.condition;
                    itemAsset.type = itemJson.TYPE;
                    itemAsset.rarity = itemJson.RARITY;
                    itemAsset.weight = RarityWeights.WeightList[itemJson.RARITY];
                    itemAsset.imageName = $"{itemJson.ID}";
                    
                    EditorUtility.SetDirty(itemAsset);
                    caseAsset.items.Add(itemAsset);

                    if (itemJson.HAS_ST == true)
                    {
                        string itemSTID = $"{itemJson.ID}-{AbbreviatedConditions.CondList[cond.condition]}st";
                        string itemSTPath = $"Assets/Resources/ItemAssets/{itemSTID}.asset";
                        ItemData itemAssetST = GetOrCreateItemAsset(itemSTID, itemSTPath);
                        int stattrakPrice = JsonPriceConversion.ToCents(cond.stattrak_price);
                        itemAssetST.id = itemSTID;
                        itemAssetST.gun = itemJson.GUN;
                        itemAssetST.name = itemJson.NAME;
                        itemAssetST.basePrice = stattrakPrice;
                        itemAssetST.price = stattrakPrice;
                        itemAssetST.condition = cond.condition;
                        itemAssetST.type = itemJson.TYPE;
                        itemAssetST.isStatTrak = itemJson.HAS_ST;
                        itemAssetST.rarity = itemJson.RARITY;
                        itemAssetST.weight = RarityWeights.WeightList[itemJson.RARITY];
                        itemAssetST.imageName = $"{itemJson.ID}";

                        EditorUtility.SetDirty(itemAssetST);
                        caseAsset.items.Add(itemAssetST);
                    }
                }
            }

            SaveAsset(caseAsset, $"Assets/Resources/CaseAssets/{caseAsset.id}.asset");
            EditorUtility.SetDirty(caseAsset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void SaveAsset(
            ScriptableObject asset,
            string path)
        {
            string directory =
                Path.GetDirectoryName(path);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) == null)
            {
                AssetDatabase.CreateAsset(asset, path);
            }

            EditorUtility.SetDirty(asset);
        }

        private static ItemData GetOrCreateItemAsset(
            string itemID,
            string path)
        {
            ItemData existing =
                AssetDatabase.LoadAssetAtPath<ItemData>(path);

            if (existing != null)
            {
                return existing;
            }

            ItemData newAsset = CreateInstance<ItemData>();

            AssetDatabase.CreateAsset(newAsset, path);

            return newAsset;
        }
    }

    [Serializable]
    public class AbbreviatedConditions
    {
        public static readonly Dictionary<string, string> CondList = new Dictionary<string, string>
        {
            {"Factory New", "fn"},
            {"Minimal Wear", "mw"},
            {"Field-Tested", "ft"},
            {"Well-Worn", "ww"},
            {"Battle-Scarred", "bs"},
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

    internal static class JsonPriceConversion
    {
        internal static int ToCents(float euroPrice)
        {
            if (float.IsNaN(euroPrice) || float.IsInfinity(euroPrice))
            {
                throw new ArgumentOutOfRangeException(nameof(euroPrice), "Price must be a finite euro amount.");
            }

            double cents = Math.Round(
                (double)euroPrice * Globals.EURO_SCALE,
                MidpointRounding.AwayFromZero);

            if (cents < int.MinValue || cents > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(euroPrice), "Price is outside the supported range.");
            }

            return (int)cents;
        }
    }
}