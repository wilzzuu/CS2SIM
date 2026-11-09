using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Graphic))]
public class ThemeElement : MonoBehaviour
{
    [SerializeField] private ThemeColorRole role = ThemeColorRole.None;

    private Graphic _graphic;

    public ThemeColorRole Role => role;

    private void OnEnable()
    {
        if (ThemeManager.Instance != null)
        {
            ThemeManager.Instance.Register(this);
        }
    }

    private void OnDisable()
    {
        if (ThemeManager.Instance != null)
        {
            ThemeManager.Instance.Unregister(this);
        }
    }

    public void SetRole(ThemeColorRole newRole)
    {
        role = newRole;
        if (ThemeManager.Instance != null)
        {
            Apply(ThemeManager.Instance.ActiveProfile);
        }
    }

    public void Apply(ThemeProfile profile)
    {
        if (profile == null)
        {
            return;
        }

        if (role == ThemeColorRole.None)
        {
            return;
        }

        if (_graphic == null)
        {
            _graphic = GetComponent<Graphic>();
        }

        Button button = GetComponent<Button>();
        if (button != null && button.targetGraphic == _graphic)
        {
            _graphic.color = Color.white;
            ColorBlock colors = button.colors;
            float fadeDuration = colors.fadeDuration;
            colors.normalColor = profile.GetColor(role);
            colors.highlightedColor = profile.AccentHover;
            colors.pressedColor = profile.AccentPressed;
            colors.selectedColor = profile.AccentSubtle;
            colors.disabledColor = profile.Surface;
            colors.fadeDuration = 0f;
            button.colors = colors;

            colors.fadeDuration = fadeDuration;
            button.colors = colors;
            return;
        }

        _graphic.color = profile.GetColor(role);
    }
}
