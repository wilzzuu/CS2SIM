using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class ClickerManager : MonoBehaviour
{
    public Button clickButton;
    public TextMeshProUGUI currencyText;
    public TextMeshProUGUI multiplierText;

    private float _currency = 0f;
    private float _sessionCurrency = 0f;
    private const float BaseGain = 0.01f;
    private float _currentMultiplier = 1f;
    private const float MaxMultiplier = 10f;
    private const float MultiplierIncrement = 0.01f;
    private const float MultiplierDecayTime = 1f;

    private Coroutine _multiplierResetCoroutine;

    public UIManager uiManager;

    void Start()
    {
        clickButton.onClick.AddListener(OnButtonClick);
        UpdateUI();
    }

    void OnButtonClick()
    {
        uiManager.LockUI();
        float earnedThisClick = BaseGain * _currentMultiplier;
        _currency += earnedThisClick;
        _sessionCurrency += earnedThisClick;

        _currentMultiplier = Mathf.Min(_currentMultiplier + MultiplierIncrement, MaxMultiplier);

        UpdateUI();

        if (_multiplierResetCoroutine != null)
        {
            StopCoroutine(_multiplierResetCoroutine);
        }
        _multiplierResetCoroutine = StartCoroutine(ResetMultiplierAfterDelay());
    }

    private IEnumerator ResetMultiplierAfterDelay()
    {
        yield return new WaitForSeconds(MultiplierDecayTime);

        PlayerManager.Instance.AddCurrency(_sessionCurrency);
        _sessionCurrency = 0f;

        _currentMultiplier = 1f;
        UpdateUI();
        uiManager.UnlockUI();
    }

    private void UpdateUI()
    {
        currencyText.text = $"Currency Gained: \n{_sessionCurrency:F2}";
        multiplierText.text = $"Multiplier: \nx{_currentMultiplier:F2}";
    }
}
