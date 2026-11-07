using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class RescueManager : MonoBehaviour
{
    public static RescueManager Instance { get; private set; }

    [SerializeField] private int cooldownHours = 4;
    [SerializeField] private int minimumBalance = 5 * Globals.EURO_SCALE;
    [SerializeField] private int rewardAmount = 200 * Globals.EURO_SCALE;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (Instance == null)
            new GameObject("RescueManager").AddComponent<RescueManager>();
    }

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

    private const string SaveFileName = "RescueData.save";

    private DateTime _nextRescueTime;

    public delegate void RescueTimerHandler();
    public event RescueTimerHandler OnRescueTimerTick;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this);
            return;
        }

        LoadData();
    }

    private void Start()
    {
        StartCoroutine(UpdateTimer());
    }

    private IEnumerator UpdateTimer()
    {
        while (true)
        {
            OnRescueTimerTick?.Invoke();
            yield return new WaitForSeconds(1f);
        }
    }

    public bool CanClaim()
    {
        return IsBalanceEligible()
            && DateTime.UtcNow >= _nextRescueTime;
    }

    public bool IsBalanceEligible()
    {
        return PlayerManager.Instance != null
            && PlayerManager.Instance.player != null
            && PlayerManager.Instance.GetPlayerBalance() <= minimumBalance;
    }

    public bool Claim()
    {
        if (!CanClaim())
            return false;

        PlayerManager.Instance.AddMoney(rewardAmount);
        LogUtil.Economy.LogEuro(
            "CLAIM_RESCUE",
            rewardAmount,
            PlayerManager.Instance.GetPlayerBalance());

        _nextRescueTime = DateTime.UtcNow.AddHours(cooldownHours);
        SaveData();
        return true;
    }

    public TimeSpan GetRemainingTime()
    {
        TimeSpan remaining = _nextRescueTime - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    public int GetRewardAmount()
    {
        return rewardAmount;
    }

    public int GetMinimumBalance()
    {
        return minimumBalance;
    }

    public void ClearData(bool delete)
    {
        string path = Path.Combine(SaveFilePath, SaveFileName);
        if (delete && File.Exists(path))
            File.Delete(path);

        LoadData();
    }

    [Serializable]
    private class RescueSaveData
    {
        public string nextRescueTime;
    }

    public void SaveData()
    {
        RescueSaveData data = new RescueSaveData
        {
            nextRescueTime = _nextRescueTime.ToString("O")
        };

        Directory.CreateDirectory(SaveFilePath);
        string path = Path.Combine(SaveFilePath, SaveFileName);
        string jsonData = JsonUtility.ToJson(data);
        string encryptedData = DataEncryptionUtility.Encrypt(jsonData);
        File.WriteAllText(path, encryptedData);
    }

    public void LoadData()
    {
        Directory.CreateDirectory(SaveFilePath);
        string path = Path.Combine(SaveFilePath, SaveFileName);
        if (!File.Exists(path))
        {
            _nextRescueTime = DateTime.MinValue;
            SaveData();
            return;
        }

        string encryptedData = File.ReadAllText(path);
        string jsonData = DataEncryptionUtility.Decrypt(encryptedData);
        RescueSaveData data = JsonUtility.FromJson<RescueSaveData>(jsonData);
        _nextRescueTime = DateTime.Parse(
            data.nextRescueTime,
            null,
            System.Globalization.DateTimeStyles.AssumeUniversal
            | System.Globalization.DateTimeStyles.AdjustToUniversal);
    }
}
