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
    private const float MinCrashPoint = 1.01f;
    private const float MaxCrashPoint = 100f;
    private const float AccelerationFactor = 0.075f;
    private float _elapsedTime = 0f;
    private float _crashPoint;
    private const float HouseEdge = 0.02f;
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
            multiplierText.text = $"x{_currentMultiplier:N2}";

            if (_currentMultiplier >= _crashPoint)
            {
                Crash();
            }
        }
    }

    public void StartCrashGame()
    {
        string _betText;
        if (betAmountInput.text.Contains("."))
        {
            _betText = betAmountInput.text.Replace(".", ",");
        }
        else
        {
            _betText = betAmountInput.text;
        }
        if (float.TryParse(_betText, out _betAmount) && _betAmount > 0 && _betAmount <= PlayerManager.Instance.GetPlayerBalance())
        {
            PlayerManager.Instance.DeductMoney(_betAmount);

            LogUtil.Economy.LogEuro(
                "PLAY_CRASH",
                -_betAmount,
                PlayerManager.Instance.GetPlayerBalance()
            );

            UpdateUI();
            ResetGame();

            _crashPoint = GenerateBiasedCrashPoint(MinCrashPoint, MaxCrashPoint, HouseEdge);
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

    private float GenerateBiasedCrashPoint(float min, float max, float houseEdge)
    {
        float r = Random.value;

        float logMin = Mathf.Log(min);
        float logMax = Mathf.Log(max);

        float t = Mathf.Pow(r, 2.4f); // main tuning knob

        float value = Mathf.Exp(Mathf.Lerp(logMin, logMax, t));

        value *= 1f - houseEdge;

        return Mathf.Clamp(value, min, max);
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
            PlayerManager.Instance.AddMoney(winnings);

            LogUtil.Economy.LogEuro(
                "WIN_CRASH",
                winnings,
                PlayerManager.Instance.GetPlayerBalance()
            );

            outcomeText.text = $"+{winnings:N2}€";
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
        multiplierText.text = $"x{_currentMultiplier:N2}";
    }
}
