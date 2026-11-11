using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ScrollPageButtons : MonoBehaviour
{
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;
    [SerializeField] private float cooldown = 0.1f;

    private float _lastScrollTime = -1f;

    void Update()
    {
        if (ItemFilterPopup.IsOpenInScene(gameObject.scene) || Mouse.current == null) return;

        float scroll = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scroll) < 0.1f) return;
        if (Time.time - _lastScrollTime < cooldown) return;

        _lastScrollTime = Time.time;

        if (scroll > 0)
            nextButton.onClick.Invoke();
        else
            prevButton.onClick.Invoke();
    }
}   