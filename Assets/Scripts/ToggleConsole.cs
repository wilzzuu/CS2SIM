using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ToggleConsole : MonoBehaviour
{
    public GameObject consoleUI;
    public InputAction toggleAction;
    public InputAction submitAction;
    private System.Action<InputAction.CallbackContext> submitCallback;
    
    private bool isToggleActive = false;

    void Awake()
    {
        toggleAction.Enable();
        submitAction.Enable();
        
        toggleAction.performed += OnTogglePerformed;
        submitCallback = context =>
        {
            if (isToggleActive)
            {
                Console.Instance.Submit();
            }
        };
        submitAction.performed += submitCallback;
    }

    void OnDestroy()
    {
        toggleAction.performed -= OnTogglePerformed;

        if (submitCallback != null)
            submitAction.performed -= submitCallback;

        toggleAction.Disable();
        submitAction.Disable();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isToggleActive = false;

        if (consoleUI != null)
            consoleUI.SetActive(false);

        if (Console.Instance != null &&
            Console.Instance.inputField != null)
        {
            Console.Instance.inputField.text = "";
            Console.Instance.inputField.DeactivateInputField();
        }
    }

    private void OnTogglePerformed(InputAction.CallbackContext context)
    {
        isToggleActive = !isToggleActive;
        
        consoleUI.SetActive(isToggleActive);
        Console.Instance.inputField.ActivateInputField();
    }
}   