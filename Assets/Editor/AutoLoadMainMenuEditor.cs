using UnityEditor;
using UnityEngine.SceneManagement;

namespace Editor
{
    [InitializeOnLoad]
    public class AutoLoadMainMenuEditor
    {
        static AutoLoadMainMenuEditor()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                SceneManager.LoadScene("MainMenu");
            }
        }
    }
}