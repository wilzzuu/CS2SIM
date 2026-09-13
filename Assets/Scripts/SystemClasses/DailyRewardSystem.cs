using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class DailyRewardSystem : MonoBehaviour
{
    public static DailyRewardSystem Instance { get; private set; }

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

    private const string SaveFileName = "DailyRewardData.save";

    public int currentDay;
    public DateTime lastClaim;

    public string dailyTimerString = "No Timer";

    public delegate void DailyTimerHandler();
    public event DailyTimerHandler OnDailyTimerTick;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        LoadData();
        ValidateStreak();

    }

    void Start()
    {
        StartCoroutine(UpdateTimer());
    }

    void ValidateStreak()
    {
        if (lastClaim == DateTime.MinValue)
            return;

        int daysPassed =
            (DateTime.UtcNow.Date - lastClaim.Date).Days;

        if (daysPassed > 1)
        {
            currentDay = 1;
            SaveData();
        }
    }

    private IEnumerator UpdateTimer()
    {
        while (true)
        {
            OnDailyTimerTick?.Invoke();
            TimeSpan remaining = GetNextAvailableTime() - DateTime.UtcNow;
            bool canClaim = remaining <= TimeSpan.Zero;
            
            if (canClaim)
            {
                dailyTimerString = "Claim!";
                AlertManager.Set(AlertType.DailyRewards, true);
            }
            else
            {
                dailyTimerString = $"{remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
                AlertManager.Set(AlertType.DailyRewards, false);
            }

            yield return new WaitForSeconds(1f);
        }
    }

    public bool CanClaimToday()
    {
        if (GetNextAvailableTime() <= DateTime.UtcNow)
            return true;

        return false;
    }

    public void GiveReward(int amount)
    {
        if (currentDay == 6)
        {   
            long cryptoAmount = (long)amount * Globals.CRYPTO_SCALE / CryptoSystem.Instance.GetEthPrice();
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
            PlayerManager.Instance.AddMoney(amount);
        
            LogUtil.Economy.LogEuro(
                "CLAIM_DAILY",
                amount,
                PlayerManager.Instance.GetPlayerBalance()
            );
        }
    }

    public DateTime GetNextAvailableTime()
    {
        return lastClaim.AddHours(24);
    }

    public void ClearData(bool delete)
    {
        if (delete)
        {
            string path = Path.Combine(SaveFilePath, SaveFileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        LoadData();
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
            currentDay = currentDay,
            lastClaim = lastClaim.ToString("O")
        };
        
        string path = Path.Combine(SaveFilePath, SaveFileName);
        string jsonData = JsonUtility.ToJson(data);
        string encryptedData = DataEncryptionUtility.Encrypt(jsonData);

        File.WriteAllText(path, encryptedData);
    }

    public void LoadData()
    {
        string path = Path.Combine(SaveFilePath, SaveFileName);
        if (!File.Exists(path))
        {
            currentDay = 0;
            lastClaim = DateTime.UtcNow;

            SaveData();

            return;
        }

        string encryptedData = File.ReadAllText(path);
        string jsonData = DataEncryptionUtility.Decrypt(encryptedData);

        DailyRewardSaveData data = JsonUtility.FromJson<DailyRewardSaveData>(jsonData);

        currentDay = data.currentDay;
        lastClaim = 
            DateTime.Parse(
                data.lastClaim,
                null,
                System.Globalization.DateTimeStyles.AssumeUniversal
                | 
                System.Globalization.DateTimeStyles.AdjustToUniversal);
    }
}