using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ToggleConsole : MonoBehaviour
{
    public GameObject consoleUI;
    public InputAction toggleAction;
    public InputAction submitAction;
    public InputAction autoCompleteAction;

    private System.Action<InputAction.CallbackContext> submitCallback;
    private System.Action<InputAction.CallbackContext> autoCompleteCallback;
    
    private bool isToggleActive = false;

    void Awake()
    {
        toggleAction.Enable();
        submitAction.Enable();
        autoCompleteAction.Enable();
        
        toggleAction.performed += OnTogglePerformed;
        submitCallback = context =>
        {
            if (isToggleActive)
            {
                Console.Instance.Submit();
            }
        };
        submitAction.performed += submitCallback;

        autoCompleteCallback = context =>
        {
            if (isToggleActive)
            {
                Console.Instance.AutoComplete();
            }
        };
        autoCompleteAction.performed += autoCompleteCallback;
    }

    void Start()
    {
        Keyboard.current.SetIMEEnabled(false);
    }

    void OnDestroy()
    {
        toggleAction.performed -= OnTogglePerformed;

        if (submitCallback != null)
            submitAction.performed -= submitCallback;
        if (autoCompleteCallback != null)
            autoCompleteAction.performed -= autoCompleteCallback;

        toggleAction.Disable();
        submitAction.Disable();
        autoCompleteAction.Disable();
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