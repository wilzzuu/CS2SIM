using System;
using System.Collections.Generic;
using UnityEngine;

public static class AlertManager
{
    private static readonly Dictionary<AlertType, bool> _states = new();

    public static event Action<AlertType, bool> OnAlertChanged;

    public static void Set(AlertType type, bool state)
    {
        if (_states.TryGetValue(type, out bool current))
        {
            if (current == state)
                return;
        }

        _states[type] = state;

        OnAlertChanged?.Invoke(type, state);
    }

    public static bool Get(AlertType type)
    {
        return _states.TryGetValue(type, out bool state) && state;
    }

    public static void Clear(AlertType type)
    {
        Set(type, false);
    }

    public static void ClearAll()
    {
        foreach (AlertType type in Enum.GetValues(typeof(AlertType)))
        {
            Set(type, false);
        }
    }
}