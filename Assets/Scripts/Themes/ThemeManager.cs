using System;
using System.Collections.Generic;
using UnityEngine;

public class ThemeManager : MonoBehaviour
{
    private const string SelectedThemeKey = "SelectedUITheme";

    private static ThemeManager _instance;
    private readonly List<ThemeProfile> _profiles = new List<ThemeProfile>();
    private readonly HashSet<ThemeElement> _elements = new HashSet<ThemeElement>();

    private ThemeProfile _activeProfile;

    public static ThemeManager Instance => _instance;
    public ThemeProfile ActiveProfile => _activeProfile;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateRuntimeManager()
    {
        if (_instance != null)
        {
            return;
        }

        GameObject managerObject = new GameObject("ThemeManager");
        managerObject.AddComponent<ThemeManager>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        LoadProfiles();
        LoadSelectedProfile();
    }

    private void OnDestroy()
    {
        if (_instance != this)
        {
            return;
        }

        _instance = null;
    }

    public bool SelectTheme(string id)
    {
        ThemeProfile profile = FindProfile(id);
        if (profile == null)
        {
            Debug.LogError($"Cannot select UI theme '{id}': no matching ThemeProfile is loaded.");
            return false;
        }

        _activeProfile = profile;
        PlayerPrefs.SetString(SelectedThemeKey, profile.Id);
        PlayerPrefs.Save();
        ApplyActiveTheme();
        return true;
    }

    public void Register(ThemeElement element)
    {
        if (element == null)
        {
            return;
        }

        _elements.Add(element);
        if (_activeProfile != null)
        {
            element.Apply(_activeProfile);
        }
    }

    public void Unregister(ThemeElement element)
    {
        _elements.Remove(element);
    }

    private void LoadProfiles()
    {
        _profiles.Clear();
        _profiles.AddRange(Resources.LoadAll<ThemeProfile>("ThemeProfiles"));

        _profiles.Sort((first, second) =>
            string.Compare(first.DisplayName, second.DisplayName, StringComparison.Ordinal));

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (ThemeProfile profile in _profiles)
        {
            if (string.IsNullOrWhiteSpace(profile.Id) || !ids.Add(profile.Id))
            {
                Debug.LogError($"Theme profile '{profile.name}' has a missing or duplicate id.");
            }
        }

        if (_profiles.Count == 0)
        {
            Debug.LogError("No UI theme profiles were found in Assets/Resources/ThemeProfiles.");
        }
    }

    private void LoadSelectedProfile()
    {
        string savedId = PlayerPrefs.GetString(SelectedThemeKey, string.Empty);
        _activeProfile = FindProfile(savedId);
        if (_activeProfile != null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(savedId))
        {
            Debug.LogWarning($"Saved UI theme '{savedId}' is unavailable. Using the default profile.");
        }

        _activeProfile = FindProfile("midnight-blue");
        if (_activeProfile == null && _profiles.Count > 0)
        {
            _activeProfile = _profiles[0];
        }

        if (_activeProfile != null)
        {
            PlayerPrefs.SetString(SelectedThemeKey, _activeProfile.Id);
            PlayerPrefs.Save();
        }
    }

    private ThemeProfile FindProfile(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        return _profiles.Find(profile =>
            string.Equals(profile.Id, id, StringComparison.Ordinal));
    }

    private void ApplyActiveTheme()
    {
        if (_activeProfile == null)
        {
            return;
        }

        _elements.RemoveWhere(element => element == null);
        foreach (ThemeElement element in _elements)
        {
            element.Apply(_activeProfile);
        }
    }
}
