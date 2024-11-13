using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class JsonToScriptableObject : MonoBehaviour
    {
        [MenuItem("Tools/Import JSON Data")]
        public static void ImportJsonData()
        {
            // Define the path to your JSON file in StreamingAssets
            string path = Application.dataPath + "/StreamingAssets/cases.json";

            if (File.Exists(path))
            {
                string jsonContent = File.ReadAllText(path);
                Debug.Log("JSON Content: " + jsonContent);

                try
                {
                    // Parse JSON into CaseJsonDataWrapper
                    CaseJsonDataWrapper caseDataWrapper = JsonUtility.FromJson<CaseJsonDataWrapper>(jsonContent);

                    // Validate parsed data
                    if (caseDataWrapper == null || caseDataWrapper.cases == null)
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
                catch (System.Exception ex)
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

                // Log each item for debugging
                Debug.Log($"Creating item: {itemJson.CONDITION} {itemJson.GUN} {itemJson.NAME} | Rarity: {itemJson.RARITY} | Price: {itemJson.PRICE} | ID: {itemJson.ID}");

                // Create ItemData ScriptableObject
                ItemData itemAsset = ScriptableObject.CreateInstance<ItemData>();
                itemAsset.id = itemJson.ID;
                itemAsset.gun = itemJson.GUN;
                itemAsset.name = itemJson.NAME;
                itemAsset.price = itemJson.PRICE;
                itemAsset.condition = itemJson.CONDITION;
                itemAsset.isStatTrak = !string.IsNullOrEmpty(itemJson.ST);
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
    }

    // Wrapper class for the cases list
    [System.Serializable]
    public class CaseJsonDataWrapper
    {
        public List<CaseJsonData> cases;
    }

    // Structure of Case JSON data
    [System.Serializable]
    public class CaseJsonData
    {
        public string ID;
        public string NAME;
        public float PRICE;
        public List<ItemJsonData> ITEMS;
    }

    // Structure of Item JSON data
    [System.Serializable]
    public class ItemJsonData
    {
        public string ID;
        public string GUN;
        public string NAME;
        public float PRICE;
        public string CONDITION;
        public string ST;
        public string TYPE;
        public string RARITY;
        public int WEIGHT;
    }
}