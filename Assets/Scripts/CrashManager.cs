using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CrashManager : MonoBehaviour
{
    public TextMeshProUGUI multiplierText;
    public TextMeshProUGUI outcomeText;
    public Button cashOutButton;
    public Button startGameButton;
    public TMP_InputField betAmountInput;

    private float _currentMultiplier = 1.0f;
    private const float InitialMultiplierRate = 0.02f;
    private const float AccelerationFactor = 0.05f;
    private float _elapsedTime = 0f;
    private float _crashPoint;
    private const float DifficultyBias = 2.15f;
    private bool _isGameRunning = false;
    private float _betAmount = 0f;
    private bool _hasCashedOut = false;


    public UIManager uiManager;

    void Start()
    {
        UpdateUI();
        cashOutButton.onClick.AddListener(CashOut);
        startGameButton.onClick.AddListener(StartCrashGame);
        cashOutButton.interactable = false;
    }

    void Update()
    {
        if (_isGameRunning && !_hasCashedOut)
        {
            _elapsedTime += Time.deltaTime;
            float currentMultiplierRate = InitialMultiplierRate + (AccelerationFactor * _elapsedTime);
            _currentMultiplier += currentMultiplierRate * Time.deltaTime;
            multiplierText.text = $"Multiplier: {_currentMultiplier:F2}x";

            if (_currentMultiplier >= _crashPoint)
            {
                Crash();
            }
        }
    }

    public void StartCrashGame()
    {
        if (float.TryParse(betAmountInput.text, out _betAmount) && _betAmount > 0 && _betAmount <= PlayerManager.Instance.GetPlayerBalance())
        {
            PlayerManager.Instance.DeductCurrency(_betAmount);
            UpdateUI();
            ResetGame();

            _crashPoint = GenerateBiasedCrashPoint(1.1f, 20.0f, DifficultyBias);
            _isGameRunning = true;
            cashOutButton.interactable = true;
            startGameButton.interactable = false;
            uiManager.LockUI();
        }
        else
        {
            Debug.LogWarning("Invalid bet amount!");
            return;
        }
    }

    private float GenerateBiasedCrashPoint(float min, float max, float bias)
    {
        float randomValue = Mathf.Pow(Random.value, bias);
        return Mathf.Lerp(min, max, randomValue);
    }

    private void ResetGame()
    {
        _currentMultiplier = 1.0f;
        _elapsedTime = 0f;
        _hasCashedOut = false;
        outcomeText.text = "";
    }

    private void Crash()
    {
        _isGameRunning = false;
        cashOutButton.interactable = false;
        startGameButton.interactable = true;
        outcomeText.text = "You crashed..";
        UpdateUI();
        uiManager.UnlockUI();
    }

    private void CashOut()
    {
        if (_isGameRunning)
        {
            float winnings = _betAmount * _currentMultiplier;
            PlayerManager.Instance.AddCurrency(winnings);
            outcomeText.text = $"You cashed out {winnings:F2}";
            _hasCashedOut = true;
            EndGame();
            uiManager.UnlockUI();
        }
    }

    private void EndGame()
    {
        _isGameRunning = false;
        cashOutButton.interactable = false;
        startGameButton.interactable = true;
        UpdateUI();
    }

    private void UpdateUI()
    {
        multiplierText.text = $"Multiplier: {_currentMultiplier:F2}x";
    }
}
