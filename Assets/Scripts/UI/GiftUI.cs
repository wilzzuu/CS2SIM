using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GiftUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Button claimButton;

    private void Start()
    {
        GiftManager.Instance.OnGiftTimerTick += UpdateTimerDisplay;
        UpdateTimerDisplay();
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (GiftManager.Instance != null)
            GiftManager.Instance.OnGiftTimerTick -= UpdateTimerDisplay;
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