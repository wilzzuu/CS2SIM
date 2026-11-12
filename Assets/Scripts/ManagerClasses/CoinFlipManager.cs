using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CoinFlipManager : MonoBehaviour
{

    [Header("UI")]
    [SerializeField] private Image coinImage;
    [SerializeField] private Sprite headsSprite;
    [SerializeField] private Sprite tailsSprite;

    [SerializeField] private TMP_Text resultText;

    [SerializeField] private TMP_InputField betInput;

    [SerializeField] private Button headsButton;
    [SerializeField] private Button tailsButton;
    [SerializeField] private Button playButton;

    [SerializeField] private Image selectionIndicatorHeads;
    [SerializeField] private Image selectionIndicatorTails;

    private enum CoinFace
    {
        Heads,
        Tails
    }

    private CoinFace _selectedFace;
    private bool _hasSelection;

    private void Start()
    {
        headsButton.onClick.AddListener(() => SelectFace(CoinFace.Heads));
        tailsButton.onClick.AddListener(() => SelectFace(CoinFace.Tails));

        playButton.interactable = false;
        playButton.onClick.AddListener(Play);

        UpdateUI(CoinFace.Heads, "Ready");
        SetSelectionVisual(false, false);
    }

    private void SelectFace(CoinFace face)
    {
        _selectedFace = face;
        _hasSelection = true;
        playButton.interactable = true;

        SetSelectionVisual(face == CoinFace.Heads, face == CoinFace.Tails);
    }

    private void Play()
    {
        if (!_hasSelection)
        {
            resultText.text = "Choose Heads or Tails!";
            return;
        }

        if (!Globals.TryParseEuroInput(betInput.text, out int bet) || bet <= 0)
        {
            resultText.text = "Invalid bet!";
            return;
        }

        if (PlayerManager.Instance.GetPlayerBalance() < bet)
        {
            resultText.text = "Not enough balance!";
            return;
        }

        PlayerManager.Instance.DeductMoney(bet);

        CoinFace outcome = Globals.AlwaysWin
            ? _selectedFace
            : (Random.value < 0.5f ? CoinFace.Heads : CoinFace.Tails);

        playButton.interactable = false;
        ShowResult(outcome, bet);
    }

    private void ShowResult(CoinFace outcome, int bet)
    {
        bool win = outcome == _selectedFace;

        UpdateUI(
            outcome,
            win ? "WIN" : "LOSE"
        );

        if (win)
        {
            int payout = bet * 2;
            PlayerManager.Instance.AddMoney(payout);

            resultText.text =
                $"WIN! +{Globals.FormatCurrencyStr(payout, Globals.EURO_DECIMAL)}€";
        }
        else
        {
            resultText.text =
                $"LOSE! -{Globals.FormatCurrencyStr(bet, Globals.EURO_DECIMAL)}€";
        }
    }

    private void UpdateUI(CoinFace face, string stateText)
    {
        switch (face)
        {
            case CoinFace.Heads:
                coinImage.sprite = headsSprite;
                break;

            case CoinFace.Tails:
                coinImage.sprite = tailsSprite;
                break;
        }

        resultText.text = stateText;
    }

    private void SetSelectionVisual(bool heads, bool tails)
    {
        selectionIndicatorHeads.enabled = heads;
        selectionIndicatorTails.enabled = tails;
    }
}