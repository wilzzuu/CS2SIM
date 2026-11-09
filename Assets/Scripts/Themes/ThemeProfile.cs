using UnityEngine;

[CreateAssetMenu(menuName = "CS2SIM/UI Theme", fileName = "ThemeProfile")]
public class ThemeProfile : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [TextArea, SerializeField] private string description;

    [Header("Surfaces")]
    [SerializeField] private Color background;
    [SerializeField] private Color surface;
    [SerializeField] private Color secondarySurface;
    [SerializeField] private Color elevatedSurface;
    [SerializeField] private Color navigationSurface;
    [SerializeField] private Color buttonSurface;
    [SerializeField] private Color border;

    [Header("Text")]
    [SerializeField] private Color primaryText;
    [SerializeField] private Color secondaryText;
    [SerializeField] private Color mutedText;

    [Header("Accent")]
    [SerializeField] private Color accent;
    [SerializeField] private Color accentHover;
    [SerializeField] private Color accentPressed;
    [SerializeField] private Color accentSubtle;

    [Header("Semantic")]
    [SerializeField] private Color success;
    [SerializeField] private Color warning;
    [SerializeField] private Color danger;
    [SerializeField] private Color information;

    public string Id => id;
    public string DisplayName => displayName;
    public string Description => description;
    public Color Background => background;
    public Color Surface => surface;
    public Color SecondarySurface => secondarySurface;
    public Color ElevatedSurface => elevatedSurface;
    public Color NavigationSurface => navigationSurface;
    public Color ButtonSurface => buttonSurface;
    public Color Border => border;
    public Color PrimaryText => primaryText;
    public Color SecondaryText => secondaryText;
    public Color MutedText => mutedText;
    public Color Accent => accent;
    public Color AccentHover => accentHover;
    public Color AccentPressed => accentPressed;
    public Color AccentSubtle => accentSubtle;
    public Color Success => success;
    public Color Warning => warning;
    public Color Danger => danger;
    public Color Information => information;

    public Color GetColor(ThemeColorRole role)
    {
        switch (role)
        {
            case ThemeColorRole.Background: return background;
            case ThemeColorRole.Surface: return surface;
            case ThemeColorRole.SecondarySurface: return secondarySurface;
            case ThemeColorRole.ElevatedSurface: return elevatedSurface;
            case ThemeColorRole.NavigationSurface: return navigationSurface;
            case ThemeColorRole.Button: return buttonSurface;
            case ThemeColorRole.Border: return border;
            case ThemeColorRole.PrimaryText: return primaryText;
            case ThemeColorRole.SecondaryText: return secondaryText;
            case ThemeColorRole.MutedText: return mutedText;
            case ThemeColorRole.Accent: return accent;
            case ThemeColorRole.AccentHover: return accentHover;
            case ThemeColorRole.AccentPressed: return accentPressed;
            case ThemeColorRole.AccentSubtle: return accentSubtle;
            case ThemeColorRole.Success: return success;
            case ThemeColorRole.Warning: return warning;
            case ThemeColorRole.Danger: return danger;
            case ThemeColorRole.Information: return information;
            default: return Color.white;
        }
    }

    private void OnValidate()
    {
        id = id?.Trim().ToLowerInvariant();
        displayName = displayName?.Trim();
    }
}
