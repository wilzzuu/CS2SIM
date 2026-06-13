using System.Collections.Generic;
using UnityEngine;

public class SidebarAlertUI : MonoBehaviour
{
    [System.Serializable]
    public class AlertReference
    {
        public AlertType type;
        public GameObject alertObject;
    }

    [SerializeField]
    private List<AlertReference> alerts = new();

    private Dictionary<AlertType, GameObject> _lookup;

    private void Awake()
    {
        _lookup = new Dictionary<AlertType, GameObject>();

        foreach (var alert in alerts)
        {
            if (alert.alertObject == null)
                continue;

            _lookup[alert.type] = alert.alertObject;
        }
    }

    private void OnEnable()
    {
        AlertManager.OnAlertChanged += HandleAlertChanged;

        RefreshAll();
    }

    private void OnDisable()
    {
        AlertManager.OnAlertChanged -= HandleAlertChanged;
    }

    private void HandleAlertChanged(AlertType type, bool state)
    {
        if (_lookup.TryGetValue(type, out GameObject alert))
        {
            alert.SetActive(state);
        }
    }

    private void RefreshAll()
    {
        foreach (var pair in _lookup)
        {
            pair.Value.SetActive(
                AlertManager.Get(pair.Key)
            );
        }
    }
}