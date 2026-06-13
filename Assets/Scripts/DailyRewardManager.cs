using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DailyRewardManager : MonoBehaviour
{
    public TextMeshProUGUI timerDateText;
    public TextMeshProUGUI timerTimeText;
    public TextMeshProUGUI timerText;

    [Serializable]
    public class DailyReward
    {
        public int day;
        public float rewardAmount;
    }

    [Header("Rewards")]
    [SerializeField] private List<DailyReward> rewards;

    [Header("UI Slots (7 elements)")]
    [SerializeField] private List<DailyRewardSlot> slots;

    private const string DailySaveKey = "DailyReward";

    private int _currentDay;
    private DateTime _lastClaim;

    private void Awake()
    {
        LoadData();
        ValidateStreak();

        foreach (var slot in slots)
        {
            int day = slot.dayIndex;
            slot.claimButton.onClick.AddListener(() => Claim(day));
        }
    }

    void Start()
    {
        StartCoroutine(UpdateTimer());
        RefreshUI();
    }

    void ValidateStreak()
    {
        if (_lastClaim == DateTime.MinValue)
            return;

        int daysPassed =
            (DateTime.UtcNow.Date - _lastClaim.Date).Days;

        if (daysPassed > 1)
        {
            _currentDay = 1;
            SaveData();
        }
    }

    private IEnumerator UpdateTimer()
    {
        while (true)
        {
            TimeSpan remaining = GetNextAvailableTime() - DateTime.UtcNow;
            bool canClaim = remaining <= TimeSpan.Zero;
            
            if (canClaim)
            {
                RefreshUI();
                timerText.text = "Ready!";
                AlertManager.Set(AlertType.DailyRewards, true);
            }
            else
            {
                timerText.text = $"{remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private void RefreshUI()
    {
        foreach (var slot in slots)
        {
            TextMeshProUGUI buttonText = slot.GetComponentInChildren<TextMeshProUGUI>();

            DailyReward reward =
                rewards.Find(r => r.day == slot.dayIndex);

            if (reward != null)
                slot.rewardText.text = $"{reward.rewardAmount:F1}€";

            if (slot.dayIndex < _currentDay)
            {
                slot.SetClaimed(buttonText);
                continue;
            }

            if (slot.dayIndex == _currentDay)
            {
                if (CanClaimToday())
                {
                    slot.SetAvailable(buttonText);
                }
                else
                {
                    slot.SetLocked(buttonText);
                }
                continue;
            }
            
            if (slot.dayIndex > _currentDay)
            {
                slot.SetLocked(buttonText);
                continue;
            }
        }
    }

    private bool CanClaimToday()
    {
        if (GetNextAvailableTime() <= DateTime.UtcNow)
            return true;

        return false;
    }

    private void Claim(int day)
    {
        if (day != _currentDay)
            return;

        if (!CanClaimToday())
            return;

        DailyReward reward =
            rewards.Find(r => r.day == day);

        GiveReward(reward.rewardAmount);
        AlertManager.Set(AlertType.DailyRewards, false);

        _lastClaim = DateTime.UtcNow;

        if (_currentDay >= 7)
            _currentDay = 1;
        else
            _currentDay++;

        SaveData();
        RefreshUI();
    }

    private void GiveReward(float amount)
    {
        if (_currentDay == 6)
        {   
            float cryptoAmount = (float)amount / CryptoSystem.Instance.GetEthPrice();
            PlayerManager.Instance.AddEth(cryptoAmount);

            LogUtil.Economy.LogCrypto(
                "CLAIM_LAST_DAILY",
                cryptoAmount,
                "ETH",
                PlayerManager.Instance.GetEthOwned()
            );
        }
        else
        {
            PlayerManager.Instance.AddCurrency(amount);
        
            LogUtil.Economy.LogEuro(
                "CLAIM_DAILY",
                amount,
                PlayerManager.Instance.GetPlayerBalance()
            );
        }
    }

    private DateTime GetNextAvailableTime()
    {
        return _lastClaim.AddHours(24);
    }

    public void ClearData()
    {
        if (PlayerPrefs.HasKey(DailySaveKey))
        {
            PlayerPrefs.DeleteKey(DailySaveKey);
            SaveData();
        }
    }

    [System.Serializable]
    private class DailyRewardSaveData
    {
        public int currentDay;
        public string lastClaim;
    }

    public void SaveData()
    {
        DailyRewardSaveData data = new DailyRewardSaveData
        {
            currentDay = _currentDay,
            lastClaim = _lastClaim.ToString("O")
        };

        PlayerPrefs.SetString(
            DailySaveKey,
            JsonUtility.ToJson(data));

        PlayerPrefs.Save();
    }

    public void LoadData()
    {
        if (!PlayerPrefs.HasKey(DailySaveKey))
        {
            _currentDay = 0;
            _lastClaim = DateTime.UtcNow;

            SaveData();

            return;
        }

        DailyRewardSaveData data = 
            JsonUtility.FromJson<DailyRewardSaveData>(
                PlayerPrefs.GetString(DailySaveKey));

        _currentDay = data.currentDay;
        _lastClaim = 
            DateTime.Parse(
                data.lastClaim,
                null,
                System.Globalization.DateTimeStyles.AssumeUniversal
                | 
                System.Globalization.DateTimeStyles.AdjustToUniversal);
    }
}