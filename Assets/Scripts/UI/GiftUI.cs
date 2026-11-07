using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GiftUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Button claimButton;

    private Button rescueButton;
    private TextMeshProUGUI rescueTimerText;

    private void Start()
    {
        GiftManager.Instance.OnGiftTimerTick += UpdateTimerDisplay;
        RescueManager.Instance.OnRescueTimerTick += RefreshRescueUI;
        PlayerManager.Instance.OnBalanceChanged += RefreshRescueUI;
        CreateRescueUI();
        UpdateTimerDisplay();
        RefreshUI();
        RefreshRescueUI();
    }

    private void OnDestroy()
    {
        if (GiftManager.Instance != null)
            GiftManager.Instance.OnGiftTimerTick -= UpdateTimerDisplay;

        if (RescueManager.Instance != null)
            RescueManager.Instance.OnRescueTimerTick -= RefreshRescueUI;

        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnBalanceChanged -= RefreshRescueUI;
    }

    private void CreateRescueUI()
    {
        Transform container = claimButton.transform.parent;

        GameObject buttonObject = new GameObject(
            "RescueButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.layer = container.gameObject.layer;
        buttonObject.transform.SetParent(container, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, -60f);
        buttonRect.sizeDelta = new Vector2(45f, 45f);

        Image rescueImage = buttonObject.GetComponent<Image>();
        Image giftImage = claimButton.GetComponent<Image>();
        rescueImage.sprite = giftImage.sprite;
        rescueImage.color = giftImage.color;

        rescueButton = buttonObject.GetComponent<Button>();
        rescueButton.transition = claimButton.transition;
        rescueButton.colors = claimButton.colors;
        rescueButton.onClick.AddListener(ClaimRescue);

        GameObject labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        labelObject.layer = container.gameObject.layer;
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = timerText.font;
        label.fontSize = Mathf.Min(timerText.fontSize, 16f);
        label.color = timerText.color;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.text = "R";

        GameObject timerObject = new GameObject(
            "RescueTimer",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        timerObject.layer = container.gameObject.layer;
        timerObject.transform.SetParent(container, false);

        RectTransform timerRect = timerObject.GetComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0.5f, 0.5f);
        timerRect.anchorMax = new Vector2(0.5f, 0.5f);
        timerRect.anchoredPosition = new Vector2(130f, -60f);
        timerRect.sizeDelta = new Vector2(250f, 50f);

        rescueTimerText = timerObject.GetComponent<TextMeshProUGUI>();
        rescueTimerText.font = timerText.font;
        rescueTimerText.fontSize = timerText.fontSize;
        rescueTimerText.color = timerText.color;
        rescueTimerText.alignment = TextAlignmentOptions.Left;
        rescueTimerText.raycastTarget = false;
    }

    private void RefreshRescueUI()
    {
        if (rescueButton == null || rescueTimerText == null || RescueManager.Instance == null)
            return;

        RescueManager manager = RescueManager.Instance;
        bool balanceEligible = manager.IsBalanceEligible();
        TimeSpan remaining = manager.GetRemainingTime();
        string reward = Globals.FormatCurrencyStr(manager.GetRewardAmount(), Globals.EURO_DECIMAL);

        rescueButton.interactable = manager.CanClaim();
        if (!balanceEligible)
        {
            rescueTimerText.text =
                $"Rescue {reward}€: max {Globals.FormatCurrencyStr(manager.GetMinimumBalance(), Globals.EURO_DECIMAL)}€";
        }
        else if (remaining > TimeSpan.Zero)
        {
            rescueTimerText.text =
                $"Rescue {reward}€: {remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }
        else
        {
            rescueTimerText.text = $"Rescue {reward}€: Claim!";
        }

        rescueTimerText.fontSize = Mathf.Min(timerText.fontSize, 14f);
    }

    private void ClaimRescue()
    {
        if (RescueManager.Instance.Claim())
            RefreshRescueUI();
    }

    private void RefreshUI()
    {
        if (claimButton != null)
        {
            claimButton.interactable = GiftManager.Instance.CanClaim();
        }
    }

    private void UpdateTimerDisplay()
    {
        string timerString = GiftManager.Instance.giftTimerString;
        timerText.text = timerString;
        if (timerString == "Claim!")
        {
            RefreshUI();
        }
    }

    public void ClaimGift()
    {
        if (DateTime.UtcNow < GiftManager.Instance.GetNextGiftTime())
            return;

        GiveReward();

        GiftManager.Instance.ResetNextGiftTime();

        RefreshUI();
    }

    private void GiveReward()
    {
        int rewardAmount = GiftManager.Instance.RewardAmount;
        PlayerManager.Instance.AddMoney(rewardAmount);

        LogUtil.Economy.LogEuro(
            "CLAIM_GIFT",
            rewardAmount,
            PlayerManager.Instance.GetPlayerBalance()
        );
    }
}