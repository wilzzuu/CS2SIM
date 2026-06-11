using System;
using System.IO;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GiftTimer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int cooldownHours = 4;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Button claimButton;

    private const float RewardAmount = 5f;

    private string SaveFilePath
    {
        get
        {

            #if UNITY_EDITOR
                return Path.Combine(Application.persistentDataPath, "EditorData");
            #else
                return Path.Combine(Application.persistentDataPath, "SaveData");
            #endif
        }
    }

    private const string SaveKey = "NextGiftTime";

    private DateTime _nextGiftTime;

    private void Awake()
    {
        LoadGiftTimer();

        if (claimButton != null)
        {
            claimButton.onClick.AddListener(ClaimGift);
        }
    }

    private void Start()
    {
        StartCoroutine(UpdateTimer());
    }

    private void OnDestroy()
    {
        if (claimButton != null)
        {
            claimButton.onClick.RemoveListener(ClaimGift);
        }
    }

    private void LoadGiftTimer()
    {
        if (PlayerPrefs.HasKey(SaveKey))
        {
            _nextGiftTime = DateTime.Parse(
                PlayerPrefs.GetString(SaveKey),
                null,
                System.Globalization.DateTimeStyles.RoundtripKind
            );
        }
        else
        {
            _nextGiftTime = DateTime.UtcNow;
        }
    }

    private void SaveGiftTimer()
    {
        PlayerPrefs.SetString(
            SaveKey,
            _nextGiftTime.ToString("O")
        );

        PlayerPrefs.Save();
    }

    private IEnumerator UpdateTimer()
    {
        while (true)
        {
            RefreshUI();

            yield return new WaitForSeconds(1f);
        }
    }

    private void RefreshUI()
    {
        TimeSpan remaining = _nextGiftTime - DateTime.UtcNow;

        bool canClaim = remaining <= TimeSpan.Zero;

        if (claimButton != null)
        {
            claimButton.interactable = canClaim;
        }

        if (timerText != null)
        {
            if (canClaim)
            {
                timerText.text = "Claim!";
            }
            else
            {
                timerText.text =
                    $"{remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
            }
        }
    }

    public void ClaimGift()
    {
        if (DateTime.UtcNow < _nextGiftTime)
            return;

        GiveReward();

        _nextGiftTime = DateTime.UtcNow.AddHours(cooldownHours);

        SaveGiftTimer();

        RefreshUI();
    }

    private void GiveReward()
    {
        PlayerManager.Instance.AddCurrency(RewardAmount);

        LogUtil.Economy.LogEuro(
            "CLAIM_GIFT",
            RewardAmount,
            PlayerManager.Instance.GetPlayerBalance()
        );
    }

    public bool CanClaim()
    {
        return DateTime.UtcNow >= _nextGiftTime;
    }

    public TimeSpan GetRemainingTime()
    {
        return _nextGiftTime - DateTime.UtcNow;
    }

    [ContextMenu("Reset Gift Timer")]
    public void ResetGiftTimer()
    {
        _nextGiftTime = DateTime.UtcNow;

        SaveGiftTimer();

        RefreshUI();

        Debug.Log("Gift timer reset.");
    }
}