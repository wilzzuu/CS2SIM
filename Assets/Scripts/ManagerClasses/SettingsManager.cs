using UnityEngine;
using UnityEngine.SceneManagement;

public class SettingsManager : MonoBehaviour
{
    public SceneSwitch sceneSwitch;
    public Transform themeSelectorPanel;
    

    public void SelectTheme(string themeId)
    {
        if (ThemeManager.Instance == null)
        {
            Debug.LogError("Cannot select a UI theme because ThemeManager is not available.");
            return;
        }

        ThemeManager.Instance.SelectTheme(themeId);
    }

    public void SettingsResetProgress()
    {
        PlayerManager.Instance.ResetProgress();
        SceneManager.LoadScene(1);
    }

    public void SettingsClaimRescue()
    {
        RescueManager.Instance.Claim();
    }

    public void SettingsToggleThemeSelector()
    {
        themeSelectorPanel.gameObject.SetActive(!themeSelectorPanel.gameObject.activeSelf);
    }
}
