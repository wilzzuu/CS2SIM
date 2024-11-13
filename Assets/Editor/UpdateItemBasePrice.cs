using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class UpdateBasePriceForAllItems : MonoBehaviour
    {
        [MenuItem("Tools/Update Item Base Prices")]
        public static void UpdateItemBasePrices()
        {
            string[] guids = AssetDatabase.FindAssets("t:ItemData"); // Find all ItemData assets

            int updatedCount = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ItemData itemData = AssetDatabase.LoadAssetAtPath<ItemData>(path);

                if (itemData != null && itemData.basePrice == 0 && itemData.price > 0)
                {
                    itemData.basePrice = itemData.price;
                    EditorUtility.SetDirty(itemData); // Mark asset as dirty for saving
                    updatedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Updated base prices for {updatedCount} items.");
        }
    }
}
