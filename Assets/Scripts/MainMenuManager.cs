using UnityEngine;
using System.IO;

public class MainMenuManager : MonoBehaviour
{
    void Start()
    {
        string saveDataDirectory = Path.Combine(Application.persistentDataPath, "SaveData");
        string editorDataDirectory = Path.Combine(Application.persistentDataPath, "EditorData");

        if (!Directory.Exists(saveDataDirectory))
        {
            Directory.CreateDirectory(saveDataDirectory);
        }

        #if UNITY_EDITOR
            if (!Directory.Exists(editorDataDirectory))
            {
                Directory.CreateDirectory(editorDataDirectory);
            }
        #endif
    }
}