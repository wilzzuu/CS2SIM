using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ItemFilterPopup : MonoBehaviour
{
    [SerializeField] private Transform conditions;
    [SerializeField] private Transform rarities;
    [SerializeField] private Transform types;
    [SerializeField] private Transform guns;
    [SerializeField] private Toggle optionTemplate;
    [SerializeField] private TMP_Text optionLabelTemplate;
    [SerializeField] private Toggle statTrak;
    [SerializeField] private Toggle nonStatTrak;
    [SerializeField] private Button clearButton;
    [SerializeField] private Button closeButton;

    private static readonly HashSet<ItemFilterPopup> OpenPopups = new HashSet<ItemFilterPopup>();
    private ItemFilter _filter;
    private Action _onChanged;

    private void OnEnable()
    {
        OpenPopups.Add(this);
    }

    private void OnDisable()
    {
        OpenPopups.Remove(this);
    }

    public static bool IsOpenInScene(Scene scene)
    {
        return OpenPopups.Any(popup => popup != null && popup.gameObject.scene == scene);
    }

    public static void Open(Transform uiElement, ItemFilter filter, IEnumerable<ItemData> items, Action onChanged)
    {
        Canvas canvas = uiElement != null ? uiElement.GetComponentInParent<Canvas>() : null;
        if (canvas == null)
        {
            Debug.LogError("Cannot open item filters without a parent Canvas.", uiElement);
            return;
        }

        canvas = canvas.rootCanvas;
        if (canvas.GetComponentInChildren<ItemFilterPopup>() != null) return;

        ItemFilterPopup prefab = Resources.Load<ItemFilterPopup>("ItemFilterPopup");
        if (prefab == null)
        {
            Debug.LogError("Cannot open item filters: Resources/ItemFilterPopup prefab is missing.");
            return;
        }

        ItemFilterPopup popup = Instantiate(prefab, canvas.transform, false);
        popup.transform.SetAsLastSibling();
        popup.Initialize(filter, items, onChanged);
    }

    public void Initialize(ItemFilter filter, IEnumerable<ItemData> items, Action onChanged)
    {
        if (conditions == null || rarities == null || types == null || guns == null ||
            optionTemplate == null || optionLabelTemplate == null || statTrak == null ||
            nonStatTrak == null || clearButton == null || closeButton == null)
        {
            Debug.LogError("Cannot open item filters: FilterPanel prefab references are incomplete.", this);
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        if (optionLabelTemplate.transform == optionTemplate.transform ||
            !optionLabelTemplate.transform.IsChildOf(optionTemplate.transform))
        {
            Debug.LogError("Cannot open item filters: option label must be a child of the option template.", this);
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        _filter = filter;
        _onChanged = onChanged;
        List<ItemData> availableItems = items.Where(item => item != null).ToList();
        AddStringGroup(conditions, availableItems.Select(item => item.condition), _filter.Conditions, ConditionData.Order);
        AddStringGroup(rarities, availableItems.Select(item => item.rarity), _filter.Rarities, RarityData.Order);
        AddStringGroup(types, availableItems.Select(item => item.type), _filter.Types);
        AddStringGroup(guns, availableItems.Select(item => item.gun), _filter.Guns);

        statTrak.SetIsOnWithoutNotify(_filter.StatTrak.Contains(true));
        nonStatTrak.SetIsOnWithoutNotify(_filter.StatTrak.Contains(false));
        statTrak.onValueChanged.AddListener(value => SetValue(_filter.StatTrak, true, value));
        nonStatTrak.onValueChanged.AddListener(value => SetValue(_filter.StatTrak, false, value));
        clearButton.onClick.AddListener(ClearFilters);
        closeButton.onClick.AddListener(Close);
    }

    private void AddStringGroup(Transform parent, IEnumerable<string> values, HashSet<string> selected,
        IReadOnlyDictionary<string, int> order = null)
    {
        foreach (string value in values.Where(value => !string.IsNullOrEmpty(value))
                     .Concat(selected.Where(value => !string.IsNullOrEmpty(value)))
                     .Distinct()
                     .OrderByDescending(value => order != null && order.TryGetValue(value, out int rank) ? rank : int.MinValue)
                     .ThenBy(value => value, StringComparer.Ordinal))
        {
            string option = value;
            Toggle toggle = Instantiate(optionTemplate, parent, false);
            toggle.name = option;
            TMP_Text label = toggle.transform.Find(
                GetRelativePath(optionTemplate.transform, optionLabelTemplate.transform)).GetComponent<TMP_Text>();
            label.text = option;
            toggle.SetIsOnWithoutNotify(selected.Contains(option));
            toggle.onValueChanged.AddListener(enabled => SetValue(selected, option, enabled));
            toggle.gameObject.SetActive(true);
        }
    }

    private static string GetRelativePath(Transform root, Transform child)
    {
        string path = child.name;
        while (child.parent != root)
        {
            child = child.parent;
            path = child.name + "/" + path;
        }
        return path;
    }

    private void SetValue<T>(HashSet<T> selected, T option, bool enabled)
    {
        if (enabled) selected.Add(option);
        else selected.Remove(option);
        _onChanged();
    }

    private void ClearFilters()
    {
        _filter.Clear();
        _onChanged();
        Close();
    }

    private void Close()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
