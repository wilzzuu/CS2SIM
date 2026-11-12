using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class CoinFlipManager : MonoBehaviour
{
    [Header("Reel")]
    [SerializeField] private RectTransform reelContainer;
    [SerializeField] private Image reelItemPrefab;
    [SerializeField] private Sprite headsSprite;
    [SerializeField] private Sprite tailsSprite;
    [SerializeField] private float spinDuration = 7f;

    [Header("Game")]
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_InputField betInput;
    [SerializeField] private Button headsButton;
    [SerializeField] private Button tailsButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Image selectionIndicatorHeads;
    [SerializeField] private Image selectionIndicatorTails;

    private const int FacesPerSide = 50;
    private const int ReelLength = FacesPerSide * 2;
    private const int ReelCycles = 2;

    private enum CoinFace
    {
        Heads,
        Tails
    }

    private readonly List<CoinFace> _faces = new List<CoinFace>(ReelLength);
    private readonly List<Image> _reelItems = new List<Image>(ReelLength * ReelCycles);
    private readonly List<RectTransform> _reelItemRects = new List<RectTransform>(ReelLength * ReelCycles);
    private GridLayoutGroup _reelLayout;
    private RectTransform _reelViewport;
    private CoinFace _selectedFace;
    private bool _hasSelection;
    private bool _isSpinning;
    private float _initialReelPositionX;

    private void Start()
    {
        if (!HasRequiredReferences())
        {
            enabled = false;
            return;
        }

        headsButton.onClick.AddListener(SelectHeads);
        tailsButton.onClick.AddListener(SelectTails);
        playButton.onClick.AddListener(Play);
        playButton.interactable = false;

        PopulateReel();
        SetSelectionVisual(false, false);
        resultText.text = "Choose Heads or Tails";
    }

    private void OnDestroy()
    {
        if (headsButton != null) headsButton.onClick.RemoveListener(SelectHeads);
        if (tailsButton != null) tailsButton.onClick.RemoveListener(SelectTails);
        if (playButton != null) playButton.onClick.RemoveListener(Play);
    }

    private bool HasRequiredReferences()
    {
        if (reelContainer != null &&
            reelContainer.parent is RectTransform &&
            reelContainer.TryGetComponent(out _reelLayout) &&
            reelItemPrefab != null &&
            headsSprite != null &&
            tailsSprite != null &&
            resultText != null &&
            betInput != null &&
            headsButton != null &&
            tailsButton != null &&
            playButton != null)
        {
            _reelViewport = (RectTransform)reelContainer.parent;
            return true;
        }

        Debug.LogError("Coin Flip is missing one or more required Inspector references.", this);
        return false;
    }

    private void SelectHeads()
    {
        SelectFace(CoinFace.Heads);
    }

    private void SelectTails()
    {
        SelectFace(CoinFace.Tails);
    }

    private void SelectFace(CoinFace face)
    {
        if (_isSpinning) return;

        _selectedFace = face;
        _hasSelection = true;
        playButton.interactable = true;
        SetSelectionVisual(face == CoinFace.Heads, face == CoinFace.Tails);
        resultText.text = "Ready";
    }

    private void Play()
    {
        if (_isSpinning) return;

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
        ShuffleReel();
        reelContainer.anchoredPosition = new Vector2(_initialReelPositionX, reelContainer.anchoredPosition.y);
        SetControlsInteractable(false);
        StartCoroutine(SpinReel(bet));
    }

    private void PopulateReel()
    {
        _faces.Clear();
        for (int i = 0; i < FacesPerSide; i++)
        {
            _faces.Add(CoinFace.Heads);
            _faces.Add(CoinFace.Tails);
        }

        ShuffleFaces();

        float itemWidth = _reelLayout.cellSize.x + _reelLayout.spacing.x;
        float totalWidth = ReelLength * ReelCycles * itemWidth - _reelLayout.spacing.x;
        reelContainer.anchorMin = new Vector2(0.5f, 0.5f);
        reelContainer.anchorMax = new Vector2(0.5f, 0.5f);
        reelContainer.pivot = new Vector2(0.5f, 0.5f);
        reelContainer.sizeDelta = new Vector2(totalWidth, reelContainer.sizeDelta.y);

        for (int i = 0; i < ReelLength * ReelCycles; i++)
        {
            Image reelItem = Instantiate(reelItemPrefab, reelContainer, false);
            reelItem.raycastTarget = false;
            reelItem.type = Image.Type.Simple;
            reelItem.preserveAspect = true;

            _reelItems.Add(reelItem);
            _reelItemRects.Add(reelItem.rectTransform);
            SetReelItemFace(i, _faces[i % ReelLength]);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(reelContainer);
        _initialReelPositionX = totalWidth / 2f - _reelViewport.rect.width / 2f;
        reelContainer.anchoredPosition = new Vector2(_initialReelPositionX, reelContainer.anchoredPosition.y);
    }

    private void ShuffleReel()
    {
        ShuffleFaces();

        for (int i = 0; i < _reelItems.Count; i++)
        {
            SetReelItemFace(i, _faces[i % ReelLength]);
        }
    }

    private void ShuffleFaces()
    {
        for (int i = _faces.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            CoinFace face = _faces[i];
            _faces[i] = _faces[randomIndex];
            _faces[randomIndex] = face;
        }
    }

    private void SetReelItemFace(int reelIndex, CoinFace face)
    {
        _reelItems[reelIndex].sprite = face == CoinFace.Heads ? headsSprite : tailsSprite;
    }

    private IEnumerator SpinReel(int bet)
    {
        _isSpinning = true;
        resultText.text = "Flipping...";

        int winningIndex = GetWinningIndex();
        float targetX = -_reelItemRects[winningIndex].localPosition.x;
        float startX = reelContainer.anchoredPosition.x;
        float elapsed = 0f;

        while (elapsed < spinDuration)
        {
            float t = elapsed / spinDuration;
            float easedT = 1f - Mathf.Pow(1f - t, 5f);
            reelContainer.anchoredPosition = new Vector2(
                Mathf.Lerp(startX, targetX, easedT),
                reelContainer.anchoredPosition.y);

            elapsed += Time.deltaTime;
            yield return null;
        }

        reelContainer.anchoredPosition = new Vector2(targetX, reelContainer.anchoredPosition.y);
        ShowResult(_faces[winningIndex % ReelLength], bet);

        _isSpinning = false;
        SetControlsInteractable(true);
    }

    private int GetWinningIndex()
    {
        int startIndex = ReelLength;
        int endIndex = ReelLength * ReelCycles;

        if (!Globals.AlwaysWin)
        {
            return Random.Range(startIndex, endIndex);
        }

        List<int> matchingIndices = new List<int>(FacesPerSide);
        for (int i = startIndex; i < endIndex; i++)
        {
            if (_faces[i % ReelLength] == _selectedFace)
            {
                matchingIndices.Add(i);
            }
        }

        return matchingIndices[Random.Range(0, matchingIndices.Count)];
    }

    private void ShowResult(CoinFace outcome, int bet)
    {
        if (outcome == _selectedFace)
        {
            int payout = bet * 2;
            PlayerManager.Instance.AddMoney(payout);
            resultText.text = $"WIN! +{Globals.FormatCurrencyStr(payout, Globals.EURO_DECIMAL)}€";
        }
        else
        {
            resultText.text = $"LOSE! -{Globals.FormatCurrencyStr(bet, Globals.EURO_DECIMAL)}€";
        }
    }

    private void SetSelectionVisual(bool heads, bool tails)
    {
        if (selectionIndicatorHeads != null) selectionIndicatorHeads.enabled = heads;
        if (selectionIndicatorTails != null) selectionIndicatorTails.enabled = tails;
    }

    private void SetControlsInteractable(bool interactable)
    {
        headsButton.interactable = interactable;
        tailsButton.interactable = interactable;
        betInput.interactable = interactable;
        playButton.interactable = interactable && _hasSelection;
    }
}
