using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AutoCaseOpener : MonoBehaviour
{
    public CaseOpening caseOpeningSystem;
    public Button[] batchSizeButtons;
    public Button unlimitedButton;
    public Button autoOpenButton;
    public TextMeshProUGUI statusText;
    public float delayBetweenOpens = 0.15f;

    private bool _isAutoOpening;
    private bool _isAutoOpenCoroutineRunning;
    private bool _unlimited;
    private int _selectedOpenCount = 5;

    private static readonly int[] BatchSizes = { 5, 10, 25, 50, 100 };

    private void Update()
    {
        SetControlsInteractable(!_isAutoOpening);
    }

    private void Start()
    {
        if (caseOpeningSystem == null)
        {
            caseOpeningSystem = FindAnyObjectByType<CaseOpening>();
        }

        if (caseOpeningSystem == null)
        {
            Debug.LogError("CaseOpening system could not be found. Auto-opening is unavailable.", this);
            Debug.Log("Case opening is unavailable.");
            SetControlsInteractable(false);
            return;
        }

        for (int i = 0; i < BatchSizes.Length; i++)
        {
            int batchSize = BatchSizes[i];
            if (batchSizeButtons != null && i < batchSizeButtons.Length && batchSizeButtons[i] != null)
            {
                batchSizeButtons[i].onClick.AddListener(() => SelectBatchSize(batchSize));
            }
        }

        if (unlimitedButton != null)
        {
            unlimitedButton.onClick.AddListener(SelectUnlimited);
        }

        if (autoOpenButton != null)
        {
            autoOpenButton.onClick.AddListener(ToggleAutoOpening);
        }

        UpdateSelectionStatus();
        SetControlsInteractable(true);
    }

    public void SelectBatchSize(int count)
    {
        if (AreSettingsLocked()) return;
        if (count != 5 && count != 10 && count != 25 && count != 50 && count != 100)
        {
            Debug.LogError($"Unsupported auto-open batch size: {count}.", this);
            return;
        }

        _selectedOpenCount = count;
        _unlimited = false;
        UpdateSelectionStatus();
    }

    public void SelectUnlimited()
    {
        if (AreSettingsLocked()) return;
        _unlimited = true;
        UpdateSelectionStatus();
    }

    public void ToggleAutoOpening()
    {
        if (_isAutoOpening)
        {
            StopAutoOpening();
            return;
        }

        if (_isAutoOpenCoroutineRunning) return;

        if (caseOpeningSystem == null)
        {
            Debug.LogError("CaseOpening system is not assigned. Auto-opening cannot start.", this);
            Debug.Log("Case opening is unavailable.");
            return;
        }

        if (!caseOpeningSystem.CanOpenCase)
        {
            Debug.Log("Select a case and ensure your balance can cover an opening.");
            return;
        }

        StartCoroutine(AutoOpenCoroutine());
    }

    public void StartAutoOpening()
    {
        if (!_isAutoOpening)
        {
            ToggleAutoOpening();
        }
    }

    public void StopAutoOpening()
    {
        _isAutoOpening = false;
        SetControlsInteractable(false);
        Debug.Log("Auto-open stopped after the current case.");
    }

    private IEnumerator AutoOpenCoroutine()
    {
        _isAutoOpening = true;
        _isAutoOpenCoroutineRunning = true;
        caseOpeningSystem.SetAutoOpening(true);
        SetControlsInteractable(false);
        SetAutoOpenButtonLabel("Stop");

        int openedCount = 0;
        bool stoppedForInsufficientFunds = false;
        while (_isAutoOpening && (_unlimited || openedCount < _selectedOpenCount))
        {
            while (_isAutoOpening && caseOpeningSystem.IsScrolling)
            {
                yield return null;
            }

            if (!_isAutoOpening) break;
            if (!caseOpeningSystem.CanOpenCase)
            {
                Debug.Log(openedCount == 0
                    ? "Select a case and ensure your balance can cover an opening."
                    : $"Auto-open stopped after {openedCount} case(s): balance is too low.");
                stoppedForInsufficientFunds = true;
                break;
            }

            if (!caseOpeningSystem.TryOpenCaseForAutoOpen())
            {
                Debug.Log(openedCount == 0
                    ? "Unable to open a case. Check your selected case and balance."
                    : $"Auto-open stopped after {openedCount} case(s): balance is too low.");
                stoppedForInsufficientFunds = true;
                break;
            }

            openedCount++;
            while (caseOpeningSystem.IsScrolling)
            {
                yield return null;
            }

            if (_isAutoOpening && (_unlimited || openedCount < _selectedOpenCount))
            {
                yield return new WaitForSeconds(delayBetweenOpens);
            }
        }

        bool stoppedByUser = !_isAutoOpening;
        _isAutoOpening = false;
        _isAutoOpenCoroutineRunning = false;
        caseOpeningSystem.SetAutoOpening(false);
        SetControlsInteractable(true);
        SetAutoOpenButtonLabel("Auto Open");
        if (openedCount > 0 && !stoppedByUser && !stoppedForInsufficientFunds)
        {
            Debug.Log($"Auto-open finished: {openedCount} case(s) opened.");
        }
    }

    private void UpdateSelectionStatus()
    {
        SetStatus(_unlimited
            ? "Mode: ∞"
            : $"Mode: {_selectedOpenCount}x");
    }

    private void SetControlsInteractable(bool interactable)
    {
        bool settingsInteractable = interactable && !AreSettingsLocked();
        if (batchSizeButtons != null)
        {
            foreach (Button button in batchSizeButtons)
            {
                if (button != null) button.interactable = settingsInteractable;
            }
        }

        if (unlimitedButton != null) unlimitedButton.interactable = settingsInteractable;
        if (autoOpenButton != null)
        {
            autoOpenButton.interactable = _isAutoOpening || (!_isAutoOpenCoroutineRunning
                && caseOpeningSystem != null && caseOpeningSystem.HasSelectedCase);
        }
    }

    private bool AreSettingsLocked()
    {
        return _isAutoOpenCoroutineRunning
            || (caseOpeningSystem != null && caseOpeningSystem.IsScrolling);
    }

    private void SetAutoOpenButtonLabel(string label)
    {
        if (autoOpenButton == null) return;
        TextMeshProUGUI buttonText = autoOpenButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null) buttonText.text = label;
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }
}
