using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
public class JsonToScriptableObject : MonoBehaviour
{
    [MenuItem("Tools/Import JSON Data")]
    public static void ImportJsonData()
    {
        // Define the path to your JSON file in StreamingAssets
        string path = Application.dataPath + "/StreamingAssets/rpg_crate_data.json";

        if (File.Exists(path))
        {
            string jsonContent = File.ReadAllText(path);
            Debug.Log("JSON Content: " + jsonContent);

            try
            {
                // Parse JSON into CrateJsonDataWrapper
                CrateJsonDataWrapper crateDataWrapper = JsonUtility.FromJson<CrateJsonDataWrapper>(jsonContent);

                // Validate parsed data
                if (crateDataWrapper == null || crateDataWrapper.Crates == null)
                {
                    Debug.LogError("Failed to parse JSON: Crate data is null or empty.");
                    return;
                }

                foreach (var crateData in crateDataWrapper.Crates)
                {
                    Debug.Log($"Processing crate: {crateData.ID} | Name: {crateData.Name} | Price: {crateData.Price}");
                    CreateCrateDataAsset(crateData);
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

    private static void CreateCrateDataAsset(CrateJsonData jsonData)
    {
        // Create CrateData ScriptableObject
        CaseData caseAsset = ScriptableObject.CreateInstance<CaseData>();
        caseAsset.ID = jsonData.ID;
        caseAsset.Name = jsonData.Name;
        caseAsset.Price = jsonData.Price;
        caseAsset.Items = new List<ItemData>();

        // Create ItemData assets and add them to the crate
        foreach (var itemJson in jsonData.Items)
        {
            if (itemJson == null)
            {
                Debug.LogWarning("Item data is missing or null.");
                continue;
            }

            // Log each item for debugging
            Debug.Log($"Creating item: {itemJson.Name} | Rarity: {itemJson.Rarity} | Price: {itemJson.Price}");

            // Create ItemData ScriptableObject
            ItemData itemAsset = ScriptableObject.CreateInstance<ItemData>();
            itemAsset.ID = itemJson.ID;
            itemAsset.Name = itemJson.Name;
            itemAsset.Price = itemJson.Price;
            itemAsset.Rarity = itemJson.Rarity;
            itemAsset.Weight = itemJson.Weight;
            itemAsset.Color = itemJson.Color;
            itemAsset.Item = itemJson.Item;
            itemAsset.Style = itemJson.Style;

            // Save each ItemData asset and add it to the CrateData
            SaveAsset(itemAsset, $"Assets/ScriptableObjects/Items/{itemAsset.ID}.asset");
            caseAsset.Items.Add(itemAsset);
        }

        // Save CrateData asset
        SaveAsset(caseAsset, $"Assets/ScriptableObjects/Crates/{jsonData.ID}.asset");
    }

    private static void SaveAsset(ScriptableObject asset, string path)
    {
        // Ensure the directory exists
        string directory = Path.GetDirectoryName(path);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Create and save the asset
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
    }
}

// Wrapper class for the crates list
[System.Serializable]
public class CrateJsonDataWrapper
{
    public List<CrateJsonData> Crates;
}

// Structure of Crate JSON data
[System.Serializable]
public class CrateJsonData
{
    public string ID;
    public string Name;
    public float Price;
    public List<ItemJsonData> Items;
}

// Structure of Item JSON data
[System.Serializable]
public class ItemJsonData
{
    public string ID;
    public string Name;
    public string Rarity;
    public float Price;
    public int Weight;
    public string Color;
    public string Item;
    public string Style;
}
