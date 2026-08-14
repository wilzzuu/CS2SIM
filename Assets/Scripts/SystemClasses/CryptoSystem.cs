using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class CryptoSystem : MonoBehaviour
{
    public static CryptoSystem Instance { get; private set; }
    public AutoTrader autoTrader;

    [Header("Market Prices")]
    public float btcPrice = 100000f;
    public float lastBtcPrice = 100000f;
    public float btcMinMarketPrice = 10000f;
    public float btcMaxMarketPrice = 500000f;
    public float ethPrice = 5000f;
    public float lastEthPrice = 5000f;
    public float ethMinMarketPrice = 500f;
    public float ethMaxMarketPrice = 25000f;

    [Header("Mining")]
    public int maxBtcMiners = 100;
    public int btcMiners = 0;
    public int maxEthMiners = 100;
    public int ethMiners = 0;
    public float btcMinerPrice = 100000f;
    public float ethMinerPrice = 5000f;
    public float btcPerHour = 0;
    public float ethPerHour = 0;

    [Header("Price Updates")]

    public DateTime lastMiningUpdate;
    public DateTime nextMiningUpdate;
    public DateTime nextMarketUpdate;
    public DateTime nextMarketEvent;
    public string currentMarketEvent = "Stable Market";

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

    private const string SaveFileName = "CryptoData.save";

    public delegate void BtcPriceHandler();
    public event BtcPriceHandler OnBtcPriceChanged;
    public delegate void BtcMinerHandler();
    public event BtcPriceHandler OnBtcMinersChanged;
    public delegate void EthPriceHandler();
    public event EthPriceHandler OnEthPriceChanged;
    public delegate void EthMinerHandler();
    public event EthPriceHandler OnEthMinersChanged;
    public delegate void CryptoEventHandler();
    public event CryptoEventHandler OnCurrentMarketEventChanged;
    
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
    }

    void Start()
    {
        ProcessOfflineProgress();
        StartCoroutine(UpdateTimer());
        StartCoroutine(UpdateMiners());
    }

    public void InvokeBtcPriceChanged()
    {
        OnBtcPriceChanged?.Invoke();
    }

    public void InvokeEthPriceChanged()
    {
        OnEthPriceChanged?.Invoke();
    }

    public void InvokeCryptoEventChanged()
    {
        nextMarketEvent = DateTime.UtcNow.AddMinutes(30);
        OnCurrentMarketEventChanged?.Invoke();
    }

    private IEnumerator UpdateTimer()
    {
        while (true)
        {
            TimeSpan remaining = nextMarketUpdate - DateTime.UtcNow;

            bool canUpdate = remaining <= TimeSpan.Zero;
            if (canUpdate)
            {
                UpdateMarketPrices();
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private void UpdateMarketPrices()
    {
        lastBtcPrice = btcPrice;
        lastEthPrice = ethPrice;

        btcPrice *= UnityEngine.Random.Range(0.99f, 1.01f);
        ethPrice *= UnityEngine.Random.Range(0.97f, 1.03f);

        btcPrice = Mathf.Max(1000f, btcPrice);
        ethPrice = Mathf.Max(100f, ethPrice);

        OnBtcPriceChanged?.Invoke();
        OnEthPriceChanged?.Invoke();
        nextMarketUpdate = DateTime.UtcNow.AddMinutes(1);

        TimeSpan remaining = nextMarketEvent - DateTime.UtcNow;
        bool canUpdate = remaining <= TimeSpan.Zero;
        if (canUpdate)
        {
            GenerateMarketEvent();
        }

        SaveData();

        //autoTrader.RecordMarketPrices();
        //autoTrader.ExecuteAutoTrader();
    }

    private IEnumerator UpdateMiners()
    {
        while (true)
        {
            TimeSpan remaining = nextMiningUpdate - DateTime.UtcNow;

            bool canUpdate = remaining <= TimeSpan.Zero;
            if (canUpdate)
            {
                if (btcMiners > 0)
                {
                    PlayerManager.Instance.MineBtc(GetBtcPerHour());
                }
                if (ethMiners > 0)
                {
                    PlayerManager.Instance.MineEth(GetEthPerHour());
                }
                nextMiningUpdate = DateTime.UtcNow.AddHours(1);

                SaveData();
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private void ProcessOfflineProgress()
    {
        DateTime now = DateTime.UtcNow;

        double hoursPassed =
            (now - lastMiningUpdate).TotalHours;


        if (hoursPassed <= 0)
            return;
        
        if (btcMiners > 0)
        {
            PlayerManager.Instance.MineBtc(GetBtcPerHour() * (float)hoursPassed);
        }
        if (ethMiners > 0)
        {
            PlayerManager.Instance.MineEth(GetEthPerHour() * (float)hoursPassed);
        }
        
        lastMiningUpdate = now;

        SaveData();
    }

    public void ResetCryptoPrices()
    {
        btcPrice = 100000f;
        ethPrice = 5000f;
        lastBtcPrice = btcPrice;
        lastEthPrice = ethPrice;
        OnBtcPriceChanged?.Invoke();
        OnEthPriceChanged?.Invoke();
        SaveData();
    }

    public void ResetBtcPrice()
    {
        btcPrice = 100000f;
        lastBtcPrice = btcPrice;
        OnBtcPriceChanged?.Invoke();
        SaveData();
    }

    public void ResetEthPrice()
    {
        ethPrice = 5000f;
        lastEthPrice = ethPrice;
        OnEthPriceChanged?.Invoke();
        SaveData();
    }

    public void AddBtcMiningRig()
    {
        if (btcMiners < maxBtcMiners)
        {
            float btcMinerPrice = GetBtcPrice();
            if (PlayerManager.Instance.GetPlayerBalance() >= btcMinerPrice)
            {
                btcMiners++;
                PlayerManager.Instance.DeductMoney(btcMinerPrice);
                GetBtcPerHour();
                OnBtcMinersChanged?.Invoke();
                SaveData();
            }
        }
    }

    public void RemoveBtcMiningRig()
    {
        if (btcMiners > 0)
        {
            btcMiners--;
            float minerSellPrice = GetBtcPrice() * 0.8f;
            PlayerManager.Instance.AddMoney(minerSellPrice);
            GetBtcPerHour();
            OnBtcMinersChanged?.Invoke();
            SaveData();
        }  
    }

    public int GetBtcMinerCount()
    {
        return btcMiners;
    }

    public void SetBtcMinerCount(int count)
    {
        btcMiners = Mathf.Clamp(count, 0, maxBtcMiners);
        GetBtcPerHour();
        OnBtcMinersChanged?.Invoke();
        SaveData();
    }

    public void AddEthMiningRig()
    {
        if (ethMiners < maxEthMiners)
        {
            float ethMinerPrice = GetEthPrice();
            if (PlayerManager.Instance.GetPlayerBalance() >= ethMinerPrice)
            {
                ethMiners++;
                PlayerManager.Instance.DeductMoney(ethMinerPrice);
                GetEthPerHour();
                OnEthMinersChanged?.Invoke();
                SaveData();
            }
        }
    }

    public void RemoveEthMiningRig()
    {
        if (ethMiners > 0)
        {
            ethMiners--;
            float minerSellPrice = GetEthPrice() * 0.8f;
            PlayerManager.Instance.AddMoney(minerSellPrice);
            GetEthPerHour();
            OnEthMinersChanged?.Invoke();
            SaveData();
        }
    }

    public int GetEthMinerCount()
    {
        return ethMiners;
    }

    public void SetEthMinerCount(int count)
    {
        ethMiners = Mathf.Clamp(count, 0, maxEthMiners);
        GetEthPerHour();
        OnEthMinersChanged?.Invoke();
        SaveData();
    }

    public float GetBtcPrice()
    {
        return btcPrice;
    }

    public float SetBtcPrice(float newPrice)
    {
        btcPrice = newPrice;
        return btcPrice;
    }

    public float GetEthPrice()
    {
        return ethPrice;
    }

    public float SetEthPrice(float newPrice)
    {
        ethPrice = newPrice;
        return ethPrice;
    }

    public float GetBtcPerHour()
    {
        if (btcMiners > 0)
        {
            btcPerHour = 1/(1200000/(1+(btcMiners-1)*0.041225f)/3600);
        }
        else
        {
            btcPerHour = 0;
        }
        return btcPerHour;
    }
    
    public float GetEthPerHour()
    {
        if (ethMiners > 0)
        {
            ethPerHour = 1/(1200000/(1+(ethMiners-1)*0.041225f)/3600);
        }
        else
        {
            ethPerHour = 0;
        }
        return ethPerHour;
    }

    #region SAVE

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
    private class CryptoSaveData
    {
        public float btcPrice;
        public float ethPrice;

        public float lastBtcPrice;
        public float lastEthPrice;

        public float btcOwned;
        public float ethOwned;

        public float btcPerHour;
        public float ethPerHour;

        public int btcMiners;
        public int ethMiners;

        public string currentMarketEvent;

        public string lastMiningUpdate;
        public string nextMiningUpdate;
        public string nextMarketUpdate;
        public string nextMarketEvent;
    }

    public void SaveData()
    {
        CryptoSaveData data = new CryptoSaveData
        {
            btcPrice = btcPrice,
            ethPrice = ethPrice,

            lastBtcPrice = lastBtcPrice,
            lastEthPrice = lastEthPrice,

            btcPerHour = btcPerHour,
            ethPerHour = ethPerHour,

            btcMiners = btcMiners,
            ethMiners = ethMiners,

            currentMarketEvent = currentMarketEvent,

            lastMiningUpdate =
                lastMiningUpdate.ToString("O"),
            nextMiningUpdate =
                nextMiningUpdate.ToString("O"),
            nextMarketUpdate = 
                nextMarketUpdate.ToString("O"),
            nextMarketEvent =
                nextMarketEvent.ToString("O"),
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
            btcPerHour = 0;
            ethPerHour = 0;
            btcMiners = 0;
            ethMiners = 0;
            currentMarketEvent = "Stable Market";
            lastMiningUpdate = DateTime.UtcNow;
            nextMiningUpdate = DateTime.UtcNow.AddHours(1);
            nextMarketUpdate = DateTime.UtcNow.AddMinutes(1);
            nextMarketEvent = DateTime.UtcNow.AddMinutes(30);

            SaveData();

            return;
        }

        string encryptedData = File.ReadAllText(path);
        string jsonData = DataEncryptionUtility.Decrypt(encryptedData);
        CryptoSaveData data = JsonUtility.FromJson<CryptoSaveData>(jsonData);

        btcPrice = data.btcPrice;
        ethPrice = data.ethPrice;

        lastBtcPrice = data.lastBtcPrice;
        lastEthPrice = data.lastEthPrice;

        btcPerHour = data.btcPerHour;
        ethPerHour = data.ethPerHour;

        btcMiners = data.btcMiners;
        ethMiners = data.ethMiners;

        currentMarketEvent = data.currentMarketEvent;

        lastMiningUpdate =
            DateTime.Parse(
                data.lastMiningUpdate, 
                null, 
                System.Globalization.DateTimeStyles.AssumeUniversal
                 | 
                System.Globalization.DateTimeStyles.AdjustToUniversal);

        nextMiningUpdate =
            DateTime.Parse(
                data.nextMiningUpdate, 
                null, 
                System.Globalization.DateTimeStyles.AssumeUniversal
                 | 
                System.Globalization.DateTimeStyles.AdjustToUniversal);

        nextMarketUpdate =
            DateTime.Parse(
                data.nextMarketUpdate, 
                null, 
                System.Globalization.DateTimeStyles.AssumeUniversal
                 | 
                System.Globalization.DateTimeStyles.AdjustToUniversal);
    
        nextMarketEvent = 
            DateTime.Parse(
                data.nextMarketEvent, 
                null, 
                System.Globalization.DateTimeStyles.AssumeUniversal
                 | 
                System.Globalization.DateTimeStyles.AdjustToUniversal);
    }

    #endregion

    #region EVENTS

    [ContextMenu("Generate Market Event")]
    public void GenerateMarketEvent()
    {
        int eventRoll = UnityEngine.Random.Range(0, 9);

        AlertManager.Set(AlertType.Crypto, true);

        switch (eventRoll)
        {
            case 0:
                ApplyBtcEvent(
                    1.15f,
                    btcMaxMarketPrice,
                    "BTC Bull Run",
                    "BTC Price +15%");
                break;

            case 1:
                ApplyBtcEvent(
                    0.85f,
                    btcMinMarketPrice,
                    "BTC Crash",
                    "BTC Price -15%");
                break;

            case 2:
                ApplyEthEvent(
                    1.20f,
                    ethMaxMarketPrice,
                    "ETH Rally",
                    "ETH Price +20%");
                break;

            case 3:
                ApplyEthEvent(
                    0.80f,
                    ethMinMarketPrice,
                    "ETH Correction",
                    "ETH Price -20%");
                break;

            case 4:
                ApplyMarketOptimism();
                break;

            case 5:
                ApplyBtcEvent(
                    0.50f,
                    btcMinMarketPrice,
                    "BTC Manipulation",
                    "BTC Price -50%");
                break;

            case 6:
                ApplyEthEvent(
                    0.50f,
                    ethMinMarketPrice,
                    "ETH Manipulation",
                    "ETH Price -50%");
                break;

            case 7:
                ApplyBtcEvent(
                    2.00f,
                    btcMaxMarketPrice,
                    "BTC Insider Trading",
                    "BTC Price +100%");
                break;

            case 8:
                ApplyEthEvent(
                    2.00f,
                    ethMaxMarketPrice,
                    "ETH Insider Trading",
                    "ETH Price +100%");
                break;
        }

        nextMarketEvent = DateTime.UtcNow.AddMinutes(30);

        OnCurrentMarketEventChanged?.Invoke();

        SaveData();
    }

    private void ApplyBtcEvent(
    float multiplier,
    float priceLimit,
    string eventName,
    string logDetails)
    {
        float newPrice = btcPrice * multiplier;

        bool withinLimit = multiplier > 1f
            ? Globals.FloatUtil.LessOrEqual(newPrice, priceLimit)
            : Globals.FloatUtil.GreaterOrEqual(newPrice, priceLimit);

        if (!withinLimit)
            return;

        btcPrice = newPrice;

        OnBtcPriceChanged?.Invoke();

        currentMarketEvent = eventName;

        LogUtil.Event.Log(
            eventName,
            logDetails);
    }

    private void ApplyEthEvent(
    float multiplier,
    float priceLimit,
    string eventName,
    string logDetails)
    {
        float newPrice = ethPrice * multiplier;

        bool withinLimit = multiplier > 1f
            ? Globals.FloatUtil.LessOrEqual(newPrice, priceLimit)
            : Globals.FloatUtil.GreaterOrEqual(newPrice, priceLimit);

        if (!withinLimit)
            return;

        ethPrice = newPrice;

        OnEthPriceChanged?.Invoke();

        currentMarketEvent = eventName;

        LogUtil.Event.Log(
            eventName,
            logDetails);
    }

    private void ApplyMarketOptimism()
    {
        float newBtcPrice = btcPrice * 1.05f;
        float newEthPrice = ethPrice * 1.05f;

        bool btcAllowed =
            Globals.FloatUtil.LessOrEqual(
                newBtcPrice,
                btcMaxMarketPrice);

        bool ethAllowed =
            Globals.FloatUtil.LessOrEqual(
                newEthPrice,
                ethMaxMarketPrice);

        if (!btcAllowed || !ethAllowed)
            return;

        btcPrice = newBtcPrice;
        ethPrice = newEthPrice;

        OnBtcPriceChanged?.Invoke();
        OnEthPriceChanged?.Invoke();

        currentMarketEvent = "Market Optimism [+5%]";

        LogUtil.Event.Log(
            "Market Optimism",
            "BTC & ETH Price +5%");
    }

    #endregion
}