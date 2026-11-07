using UnityEngine;
using System.IO;

public class MainMenuManager : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeSaveDirectories()
    {
        string saveDataDirectory = Path.Combine(Application.persistentDataPath, "SaveData");
        string editorDataDirectory = Path.Combine(Application.persistentDataPath, "EditorData");

        Directory.CreateDirectory(saveDataDirectory);

        #if UNITY_EDITOR
            Directory.CreateDirectory(editorDataDirectory);
        #endif
    }
}