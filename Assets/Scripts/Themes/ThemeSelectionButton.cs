using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ThemeSelectionButton : MonoBehaviour
{
    [SerializeField] private ThemeProfile themeProfile;
    [SerializeField] private Image themeColorImage;
    [SerializeField] private TMP_Text nameText;

    private void Awake()
    {
        UpdateAppearance();
    }

    private void OnValidate()
    {
        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        if (themeProfile == null)
        {
            return;
        }

        if (themeColorImage != null)
        {
            themeColorImage.color = themeProfile.Accent;
        }

        if (nameText != null)
        {
            nameText.text = themeProfile.DisplayName;
            nameText.color = themeProfile.PrimaryText;
        }
    }

    public void SelectTheme()
    {
        if (themeProfile == null)
        {
            Debug.LogError($"Cannot select a UI theme from '{name}': no ThemeProfile is assigned.", this);
            return;
        }

        if (ThemeManager.Instance == null)
        {
            Debug.LogError("Cannot select a UI theme because ThemeManager is not available.", this);
            return;
        }

        ThemeManager.Instance.SelectTheme(themeProfile.Id);
    }
}
