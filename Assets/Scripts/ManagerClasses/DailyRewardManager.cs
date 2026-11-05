using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DailyRewardUI : MonoBehaviour
{
    public TextMeshProUGUI timerDateText;
    public TextMeshProUGUI timerTimeText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI seventhSlotEthValueText;

    [Serializable]
    public class DailyReward
    {
        public int day;
        public int rewardAmount;
    }

    [Header("Rewards")]
    [SerializeField] private List<DailyReward> rewards;

    [Header("UI Slots (7 elements)")]
    [SerializeField] private List<DailyRewardSlot> slots;

    private void Awake()
    {
        foreach (var slot in slots)
        {
            int day = slot.dayIndex;
            slot.claimButton.onClick.AddListener(() => Claim(day));
        }
    }

    void Start()
    {
        DailyRewardSystem.Instance.OnDailyTimerTick += UpdateTimerDisplay;
        UpdateTimerDisplay();
        RefreshUI();
    }

    void OnDestroy()
    {
        if (DailyRewardSystem.Instance != null)
        {
            DailyRewardSystem.Instance.OnDailyTimerTick -= UpdateTimerDisplay;
        }
    }

    private void RefreshUI()
    {
        long ethValue = (long)rewards[6].rewardAmount * Globals.CRYPTO_SCALE / CryptoSystem.Instance.GetEthPrice();
        seventhSlotEthValueText.text = $"{Globals.FormatCurrencyStr(ethValue, Globals.CRYPTO_DECIMAL)}";
        foreach (var slot in slots)
        {
            TextMeshProUGUI buttonText = slot.GetComponentInChildren<TextMeshProUGUI>();

            DailyReward reward =
                rewards.Find(r => r.day == slot.dayIndex);

            if (reward != null)
                slot.rewardText.text = $"{reward.rewardAmount}€";

            if (slot.dayIndex < DailyRewardSystem.Instance.currentDay)
            {
                slot.SetClaimed(buttonText);
                continue;
            }

            if (slot.dayIndex == DailyRewardSystem.Instance.currentDay)
            {
                if (DailyRewardSystem.Instance.CanClaimToday())
                {
                    slot.SetAvailable(buttonText);
                }
                else
                {
                    slot.SetLocked(buttonText);
                }
                continue;
            }
            
            if (slot.dayIndex > DailyRewardSystem.Instance.currentDay)
            {
                slot.SetLocked(buttonText);
                continue;
            }
        }
    }

    private void UpdateTimerDisplay()
    {
        string timerString = DailyRewardSystem.Instance.dailyTimerString;
        timerText.text = timerString;
        if (timerString == "Claim!")
        {
            RefreshUI();
        }
    }

    private void Claim(int day)
    {
        if (day != DailyRewardSystem.Instance.currentDay)
            return;

        if (!DailyRewardSystem.Instance.CanClaimToday())
            return;

        DailyReward reward =
            rewards.Find(r => r.day == day);

        DailyRewardSystem.Instance.GiveReward(reward.rewardAmount);
        AlertManager.Set(AlertType.DailyRewards, false);

        DailyRewardSystem.Instance.lastClaim = DateTime.UtcNow;
        
        DailyRewardSystem.Instance.currentDay++;
        if (DailyRewardSystem.Instance.currentDay >= 7)
            DailyRewardSystem.Instance.currentDay = 0;

        DailyRewardSystem.Instance.SaveData();
        RefreshUI();
    }
}