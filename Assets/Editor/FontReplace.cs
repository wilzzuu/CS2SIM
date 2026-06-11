using TMPro;
using UnityEditor;
using UnityEngine;

public class ReplaceTMPFonts
{
    [MenuItem("Tools/Replace TMP Fonts")]
    static void ReplaceFonts()
    {
        TMP_FontAsset newFont =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Fonts/MyFont SDF.asset"
            );

        TMP_Text[] texts =
            Object.FindObjectsByType<TMP_Text>();

        int count = 0;

        foreach (TMP_Text text in texts)
        {
            Undo.RecordObject(text, "Replace Font");

            text.font = newFont;

            EditorUtility.SetDirty(text);

            count++;
        }

        Debug.Log($"Updated {count} TMP texts.");
    }
}