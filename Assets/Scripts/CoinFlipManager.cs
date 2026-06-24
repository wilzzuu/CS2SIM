using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CoinFlipManager : MonoBehaviour
{
    public static CoinFlipManager Instance;

    [Header("UI")]
    [SerializeField] private Image coinImage;
    [SerializeField] private Sprite headsSprite;
    [SerializeField] private Sprite tailsSprite;

    [SerializeField] private TMP_Text faceNameText;
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

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        headsButton.onClick.AddListener(() => SelectFace(CoinFace.Heads));
        tailsButton.onClick.AddListener(() => SelectFace(CoinFace.Tails));
        playButton.onClick.AddListener(Play);

        UpdateUI(CoinFace.Heads, "Ready");
        SetSelectionVisual(false, false);
    }

    private void SelectFace(CoinFace face)
    {
        _selectedFace = face;
        _hasSelection = true;

        SetSelectionVisual(face == CoinFace.Heads, face == CoinFace.Tails);
    }

    private void Play()
    {
        if (!_hasSelection)
        {
            resultText.text = "Choose Heads or Tails!";
            return;
        }

        if (!float.TryParse(betInput.text, out float bet) || bet <= 0)
        {
            resultText.text = "Invalid bet!";
            return;
        }

        if (Globals.FloatUtil.Round(PlayerManager.Instance.GetPlayerBalance()) < Globals.FloatUtil.Round(bet))
        {
            resultText.text = "Not enough balance!";
            return;
        }

        PlayerManager.Instance.DeductCurrency(bet);

        CoinFace outcome = (Random.value < 0.5f)
            ? CoinFace.Heads
            : CoinFace.Tails;

        ShowResult(outcome, bet);
    }

    private void ShowResult(CoinFace outcome, float bet)
    {
        bool win = outcome == _selectedFace;

        UpdateUI(
            outcome,
            win ? "WIN" : "LOSE"
        );

        if (win)
        {
            float payout = bet * 2f;
            PlayerManager.Instance.AddCurrency(payout);

            resultText.text =
                $"WIN! +{payout:F2}€";
        }
        else
        {
            resultText.text =
                $"LOSE! -{bet:F2}€";
        }
    }

    private void UpdateUI(CoinFace face, string stateText)
    {
        switch (face)
        {
            case CoinFace.Heads:
                coinImage.sprite = headsSprite;
                faceNameText.text = "HEADS";
                break;

            case CoinFace.Tails:
                coinImage.sprite = tailsSprite;
                faceNameText.text = "TAILS";
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