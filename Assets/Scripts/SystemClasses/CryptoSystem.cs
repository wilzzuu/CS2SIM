using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class CryptoSystem : MonoBehaviour
{
    public static CryptoSystem Instance { get; private set; }
    public AutoTrader autoTrader;

    [Header("Market Prices")]
    public int btcPrice = 100000 * Globals.EURO_SCALE;
    public int lastBtcPrice = 100000 * Globals.EURO_SCALE;
    public int btcMinMarketPrice = 10000 * Globals.EURO_SCALE;
    public int btcMaxMarketPrice = 500000 * Globals.EURO_SCALE;
    public int ethPrice = 5000 * Globals.EURO_SCALE;
    public int lastEthPrice = 5000 * Globals.EURO_SCALE;
    public int ethMinMarketPrice = 500 * Globals.EURO_SCALE;
    public int ethMaxMarketPrice = 25000 * Globals.EURO_SCALE;

    [Header("Mining")]
    public int maxBtcMiners = 100;
    public int btcMiners = 0;
    public int maxEthMiners = 100;
    public int ethMiners = 0;
    public int btcMinerPrice = 100000 * Globals.EURO_SCALE;
    public int ethMinerPrice = 5000 * Globals.EURO_SCALE;
    public long btcPerHour = 0;
    public long ethPerHour = 0;

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

            yield return new WaitForSecondsRealtime(1f);
        }
    }

    private void UpdateMarketPrices()
    {
        lastBtcPrice = btcPrice;
        lastEthPrice = ethPrice;

        btcPrice = Globals.MoneyUtil.RoundEuro(btcPrice * UnityEngine.Random.Range(0.99f, 1.01f));
        ethPrice = Globals.MoneyUtil.RoundEuro(ethPrice * UnityEngine.Random.Range(0.97f, 1.03f));

        btcPrice = Mathf.Max(1000 * Globals.EURO_SCALE, btcPrice);
        ethPrice = Mathf.Max(100 * Globals.EURO_SCALE, ethPrice);

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

            yield return new WaitForSecondsRealtime(1f);
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
            PlayerManager.Instance.MineBtc(Globals.MoneyUtil.RoundCryptoUnits(GetBtcPerHour() * hoursPassed));
        }
        if (ethMiners > 0)
        {
            PlayerManager.Instance.MineEth(Globals.MoneyUtil.RoundCryptoUnits(GetEthPerHour() * hoursPassed));
        }
        
        lastMiningUpdate = now;

        SaveData();
    }

    public void ResetCryptoPrices()
    {
        btcPrice = 100000 * Globals.EURO_SCALE;
        ethPrice = 5000 * Globals.EURO_SCALE;
        lastBtcPrice = btcPrice;
        lastEthPrice = ethPrice;
        OnBtcPriceChanged?.Invoke();
        OnEthPriceChanged?.Invoke();
        SaveData();
    }

    public void ResetBtcPrice()
    {
        btcPrice = 100000 * Globals.EURO_SCALE;
        lastBtcPrice = btcPrice;
        OnBtcPriceChanged?.Invoke();
        SaveData();
    }

    public void ResetEthPrice()
    {
        ethPrice = 5000 * Globals.EURO_SCALE;
        lastEthPrice = ethPrice;
        OnEthPriceChanged?.Invoke();
        SaveData();
    }

    public void AddBtcMiningRig()
    {
        if (btcMiners < maxBtcMiners)
        {
            int btcMinerPrice = GetBtcPrice();
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
            int minerSellPrice = Globals.MoneyUtil.RoundEuro(GetBtcPrice() * 0.8f);
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
            int ethMinerPrice = GetEthPrice();
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
            int minerSellPrice = Globals.MoneyUtil.RoundEuro(GetEthPrice() * 0.8f);
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

    public int GetBtcPrice()
    {
        return btcPrice;
    }

    public int SetBtcPrice(int newPrice)
    {
        btcPrice = newPrice;
        return btcPrice;
    }

    public int GetEthPrice()
    {
        return ethPrice;
    }

    public int SetEthPrice(int newPrice)
    {
        ethPrice = newPrice;
        return ethPrice;
    }

    public long GetBtcPerHour()
    {
        if (btcMiners > 0)
        {
            btcPerHour = Globals.MoneyUtil.RoundCryptoUnits(
                (1 / (1200000 / (1 + (btcMiners - 1) * 0.041225f) / 3600)) * Globals.CRYPTO_SCALE);
        }
        else
        {
            btcPerHour = 0;
        }
        return btcPerHour;
    }
    
    public long GetEthPerHour()
    {
        if (ethMiners > 0)
        {
            ethPerHour = Globals.MoneyUtil.RoundCryptoUnits(
                (1 / (1200000 / (1 + (ethMiners - 1) * 0.041225f) / 3600)) * Globals.CRYPTO_SCALE);
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
        public int btcPrice;
        public int ethPrice;

        public int lastBtcPrice;
        public int lastEthPrice;

        public long btcOwned;
        public long ethOwned;

        public long btcPerHour;
        public long ethPerHour;

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

            lastMiningUpdate = Globals.FormatUtcDateTime(lastMiningUpdate),
            nextMiningUpdate = Globals.FormatUtcDateTime(nextMiningUpdate),
            nextMarketUpdate = Globals.FormatUtcDateTime(nextMarketUpdate),
            nextMarketEvent = Globals.FormatUtcDateTime(nextMarketEvent),
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

        lastMiningUpdate = Globals.ParseUtcDateTime(data.lastMiningUpdate);
        nextMiningUpdate = Globals.ParseUtcDateTime(data.nextMiningUpdate);
        nextMarketUpdate = Globals.ParseUtcDateTime(data.nextMarketUpdate);
        nextMarketEvent = Globals.ParseUtcDateTime(data.nextMarketEvent);
    }

    #endregion

    #region EVENTS

    [ContextMenu("Generate Market Event")]
    public void GenerateMarketEvent()
    {
        int eventRoll = UnityEngine.Random.Range(0, 9);

        AlertManager.Set(AlertType.Crypto, true);
        string previousEvent = currentMarketEvent;

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

        if (currentMarketEvent != previousEvent)
            OnCurrentMarketEventChanged?.Invoke();
        SaveData();
    }

    private void ApplyBtcEvent(
    float multiplier,
    int priceLimit,
    string eventName,
    string logDetails)
    {
        int newPrice = Globals.MoneyUtil.RoundEuro(btcPrice * multiplier);

        bool withinLimit = multiplier > 1f
            ? newPrice <= priceLimit
            : newPrice >= priceLimit;

        if (!withinLimit)
            return;

        btcPrice = newPrice;

        OnBtcPriceChanged?.Invoke();

        currentMarketEvent = $"{eventName} ({multiplier * 100f - 100f:+0;-0}%)";

        LogUtil.Event.Log(
            eventName,
            logDetails);
    }

    private void ApplyEthEvent(
    float multiplier,
    int priceLimit,
    string eventName,
    string logDetails)
    {
        int newPrice = Globals.MoneyUtil.RoundEuro(ethPrice * multiplier);

        bool withinLimit = multiplier > 1f
            ? newPrice <= priceLimit
            : newPrice >= priceLimit;

        if (!withinLimit)
            return;

        ethPrice = newPrice;

        OnEthPriceChanged?.Invoke();

        currentMarketEvent = $"{eventName} ({multiplier * 100f - 100f:+0;-0}%)";

        LogUtil.Event.Log(
            eventName,
            logDetails);
    }

    private void ApplyMarketOptimism()
    {
        int newBtcPrice = Globals.MoneyUtil.RoundEuro(btcPrice * 1.05f);
        int newEthPrice = Globals.MoneyUtil.RoundEuro(ethPrice * 1.05f);

        bool btcAllowed = newBtcPrice <= btcMaxMarketPrice;

        bool ethAllowed = newEthPrice <= ethMaxMarketPrice;

        if (!btcAllowed || !ethAllowed)
            return;

        btcPrice = newBtcPrice;
        ethPrice = newEthPrice;

        OnBtcPriceChanged?.Invoke();
        OnEthPriceChanged?.Invoke();

        currentMarketEvent = "Market Optimism (BTC & ETH +5%)";

        LogUtil.Event.Log(
            "Market Optimism",
            "BTC & ETH Price +5%");
    }

    #endregion
}