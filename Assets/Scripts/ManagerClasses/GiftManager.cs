using System;
using System.IO;
using System.Collections;
using UnityEngine;

public class GiftManager : MonoBehaviour
{
    public static GiftManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int cooldownHours = 2;

    public int RewardAmount = 500 * Globals.EURO_SCALE;

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

    private const string SaveFileName = "GiftData.save";

    public string giftTimerString = "No Timer";

    public delegate void GiftTimerHandler();
    public event GiftTimerHandler OnGiftTimerTick;

    private DateTime _nextGiftTime;

    private void Awake()
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
    }

    private void Start()
    {
        StartCoroutine(UpdateTimer());
    }

    private IEnumerator UpdateTimer()
    {
        while (true)
        {
            OnGiftTimerTick?.Invoke();
            TimeSpan remaining = _nextGiftTime - DateTime.UtcNow;
            bool canClaim = remaining <= TimeSpan.Zero;

            if (canClaim)
            {
                giftTimerString = "Claim!";
            }
            else
            {
                giftTimerString = $"{remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
            }

            yield return new WaitForSeconds(1f);
        }
    }

    public bool CanClaim()
    {
        return DateTime.UtcNow >= _nextGiftTime;
    }

    public TimeSpan GetRemainingTime()
    {
        return _nextGiftTime - DateTime.UtcNow;
    }

    public DateTime GetNextGiftTime()
    {
        return _nextGiftTime;
    }

    public void ResetNextGiftTime()
    {
        _nextGiftTime = DateTime.UtcNow.AddHours(cooldownHours);
        SaveData();
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
    private class GiftSaveData
    {
        public string nextGiftTime;
    }

    public void SaveData()
    {
        GiftSaveData data = new GiftSaveData
        {
            nextGiftTime = _nextGiftTime.ToString("O")
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
            _nextGiftTime = DateTime.UtcNow.AddHours(cooldownHours);

            SaveData();

            return;
        }

        string encryptedData = File.ReadAllText(path);
        string jsonData = DataEncryptionUtility.Decrypt(encryptedData);

        GiftSaveData data = JsonUtility.FromJson<GiftSaveData>(jsonData);
        _nextGiftTime =
            DateTime.Parse(
                data.nextGiftTime,
                null,
                System.Globalization.DateTimeStyles.AssumeUniversal
                | 
                System.Globalization.DateTimeStyles.AdjustToUniversal);
    }
}