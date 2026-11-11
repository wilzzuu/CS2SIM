using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class ItemFilterButton : MonoBehaviour
{
    private Button _button;

    private void OnEnable()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OpenFilters);
    }

    private void OnDisable()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(OpenFilters);
        }
    }

    public void OpenFilters()
    {
        IItemFilterScreen screen = null;
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(false))
            {
                if (!behaviour || !behaviour.isActiveAndEnabled || !(behaviour is IItemFilterScreen candidate))
                {
                    continue;
                }

                if (screen != null)
                {
                    Debug.LogError("Cannot open item filters: more than one active filter screen exists in this scene.", this);
                    return;
                }

                screen = candidate;
            }
        }

        if (screen == null)
        {
            Debug.LogError("Cannot open item filters: no active filter screen exists in this scene.", this);
            return;
        }

        screen.OpenFilters();
    }
}
