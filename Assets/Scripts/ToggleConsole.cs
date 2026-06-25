using UnityEngine;
using UnityEngine.InputSystem;

public class ToggleConsole : MonoBehaviour
{
    public GameObject consoleUI;
    public InputAction toggleAction;
    public InputAction submitAction;
    
    private bool isToggleActive = false;

    void Awake()
    {
        toggleAction.Enable();
        submitAction.Enable();
        
        toggleAction.performed += OnTogglePerformed;
        submitAction.performed += context =>
        {
            if (isToggleActive)
            {
                Console.Instance.Submit();
            }
        };
    }

    void OnDestroy()
    {
        toggleAction.performed -= OnTogglePerformed;
        submitAction.performed -= context =>
        {
            if (isToggleActive)
            {
                Console.Instance.Submit();
            }
        };
        toggleAction.Disable();
        submitAction.Disable();
    }

    private void OnTogglePerformed(InputAction.CallbackContext context)
    {
        isToggleActive = !isToggleActive;
        
        consoleUI.SetActive(isToggleActive);
        Console.Instance.inputField.ActivateInputField();
    }
}   