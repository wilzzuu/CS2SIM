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
    public float ethPrice = 5000f;
    public float lastEthPrice = 5000f;
    public float ethMinMarketPrice = 500f;
    private float _marketEventChance = 10f;

    [Header("Mining")]
    public int maxBtcMiners = 100;
    public int _btcMiners = 0;
    public int maxEthMiners = 100;
    public int _ethMiners = 0;
    public float btcMinerPrice = 100000f;
    public float ethMinerPrice = 5000f;
    public float _btcPerHour = 0;
    public float _ethPerHour = 0;

    [Header("Price Updates")]

    private DateTime _lastMiningUpdate;
    private DateTime _nextMiningUpdate;
    private DateTime _nextMarketUpdate;
    private DateTime _nextMarketEvent;
    public string currentMarketEvent = "No Event";

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

    private IEnumerator UpdateTimer()
    {
        while (true)
        {
            TimeSpan remaining = _nextMarketUpdate - DateTime.UtcNow;

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
        _nextMarketUpdate = DateTime.UtcNow.AddMinutes(1);

        SaveData();

        TimeSpan remaining = _nextMarketEvent - DateTime.UtcNow;
        bool canUpdate = remaining <= TimeSpan.Zero;
        if (canUpdate)
        {
            bool isMarketEvent = UnityEngine.Random.Range(0, 100) < _marketEventChance;
            if (isMarketEvent)
            {
                GenerateMarketEvent();
                _nextMarketEvent = DateTime.UtcNow.AddMinutes(30);
            }
            else
            {
                currentMarketEvent = "No Event";
            }
            OnCurrentMarketEventChanged?.Invoke();
        }

        //autoTrader.RecordMarketPrices();
        //autoTrader.ExecuteAutoTrader();
    }

    private IEnumerator UpdateMiners()
    {
        while (true)
        {
            TimeSpan remaining = _nextMiningUpdate - DateTime.UtcNow;

            bool canUpdate = remaining <= TimeSpan.Zero;
            if (canUpdate)
            {
                if (_btcMiners > 0)
                {
                    PlayerManager.Instance.MineBtc(GetBtcPerHour());
                }
                if (_ethMiners > 0)
                {
                    PlayerManager.Instance.MineEth(GetEthPerHour());
                }
                _nextMiningUpdate = DateTime.UtcNow.AddHours(1);

                SaveData();
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private void ProcessOfflineProgress()
    {
        DateTime now = DateTime.UtcNow;

        double hoursPassed =
            (now - _lastMiningUpdate).TotalHours;


        if (hoursPassed <= 0)
            return;
        
        if (_btcMiners > 0)
        {
            PlayerManager.Instance.MineBtc(GetBtcPerHour() * (float)hoursPassed);
        }
        if (_ethMiners > 0)
        {
            PlayerManager.Instance.MineEth(GetEthPerHour() * (float)hoursPassed);
        }
        
        _lastMiningUpdate = now;

        SaveData();
    }

    public void AddBtcMiningRig()
    {
        if (_btcMiners < maxBtcMiners)
        {
            float btcMinerPrice = GetBtcPrice();
            if (PlayerManager.Instance.GetPlayerBalance() >= btcMinerPrice)
            {
                _btcMiners++;
                PlayerManager.Instance.DeductMoney(btcMinerPrice);
                GetBtcPerHour();
                OnBtcMinersChanged?.Invoke();
                SaveData();
            }
        }
    }

    public void RemoveBtcMiningRig()
    {
        if (_btcMiners > 0)
        {
            _btcMiners--;
            float minerSellPrice = GetBtcPrice() * 0.8f;
            PlayerManager.Instance.AddMoney(minerSellPrice);
            GetBtcPerHour();
            OnBtcMinersChanged?.Invoke();
            SaveData();
        }  
    }

    public int GetBtcMinerCount()
    {
        return _btcMiners;
    }

    public void SetBtcMinerCount(int count)
    {
        _btcMiners = Mathf.Clamp(count, 0, maxBtcMiners);
        GetBtcPerHour();
        OnBtcMinersChanged?.Invoke();
        SaveData();
    }

    public void AddEthMiningRig()
    {
        if (_ethMiners < maxEthMiners)
        {
            float ethMinerPrice = GetEthPrice();
            if (PlayerManager.Instance.GetPlayerBalance() >= ethMinerPrice)
            {
                _ethMiners++;
                PlayerManager.Instance.DeductMoney(ethMinerPrice);
                GetEthPerHour();
                OnEthMinersChanged?.Invoke();
                SaveData();
            }
        }
    }

    public void RemoveEthMiningRig()
    {
        if (_ethMiners > 0)
        {
            _ethMiners--;
            float minerSellPrice = GetEthPrice() * 0.8f;
            PlayerManager.Instance.AddMoney(minerSellPrice);
            GetEthPerHour();
            OnEthMinersChanged?.Invoke();
            SaveData();
        }
    }

    public int GetEthMinerCount()
    {
        return _ethMiners;
    }

    public void SetEthMinerCount(int count)
    {
        _ethMiners = Mathf.Clamp(count, 0, maxEthMiners);
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
        if (_btcMiners > 0)
        {
            _btcPerHour = 1/(1200000/(1+(_btcMiners-1)*0.041225f)/3600);
        }
        else
        {
            _btcPerHour = 0;
        }
        return _btcPerHour;
    }
    
    public float GetEthPerHour()
    {
        if (_ethMiners > 0)
        {
            _ethPerHour = 1/(1200000/(1+(_ethMiners-1)*0.041225f)/3600);
        }
        else
        {
            _ethPerHour = 0;
        }
        return _ethPerHour;
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

            btcPerHour = _btcPerHour,
            ethPerHour = _ethPerHour,

            btcMiners = _btcMiners,
            ethMiners = _ethMiners,

            lastMiningUpdate =
                _lastMiningUpdate.ToString("O"),
            nextMiningUpdate =
                _nextMiningUpdate.ToString("O"),
            nextMarketUpdate = 
                _nextMarketUpdate.ToString("O"),
            nextMarketEvent =
                _nextMarketEvent.ToString("O")
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
            _btcPerHour = 0;
            _ethPerHour = 0;
            _btcMiners = 0;
            _ethMiners = 0;
            _lastMiningUpdate = DateTime.UtcNow;
            _nextMiningUpdate = DateTime.UtcNow;
            _nextMarketUpdate = DateTime.UtcNow;
            _nextMarketEvent = DateTime.UtcNow;

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

        _btcPerHour = data.btcPerHour;
        _ethPerHour = data.ethPerHour;

        _btcMiners = data.btcMiners;
        _ethMiners = data.ethMiners;

        _lastMiningUpdate =
            DateTime.Parse(
                data.lastMiningUpdate, 
                null, 
                System.Globalization.DateTimeStyles.AssumeUniversal
                 | 
                System.Globalization.DateTimeStyles.AdjustToUniversal);

        _nextMiningUpdate =
            DateTime.Parse(
                data.nextMiningUpdate, 
                null, 
                System.Globalization.DateTimeStyles.AssumeUniversal
                 | 
                System.Globalization.DateTimeStyles.AdjustToUniversal);

        _nextMarketUpdate =
            DateTime.Parse(
                data.nextMarketUpdate, 
                null, 
                System.Globalization.DateTimeStyles.AssumeUniversal
                 | 
                System.Globalization.DateTimeStyles.AdjustToUniversal);
    
        _nextMarketEvent = 
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
        int eventRoll = UnityEngine.Random.Range(0, 8);
        AlertManager.Set(AlertType.Crypto, true);

        switch (eventRoll)
        {
            case 0:
                btcPrice *= 1.15f;
                OnBtcPriceChanged?.Invoke();
                currentMarketEvent = "BTC Bull Run [+15%]";
                LogUtil.Event.Log(
                    "BTC Bull Run",
                    "BTC Price +15%"
                );
                break;

            case 1:
                if (Globals.FloatUtil.GreaterOrEqual(btcPrice * 0.85f, btcMinMarketPrice))
                {
                    btcPrice *= 0.85f;
                    OnBtcPriceChanged?.Invoke();
                    currentMarketEvent = "BTC Crash [-15%]";
                    LogUtil.Event.Log(
                        "BTC Crash",
                        "BTC Price -15%"
                    );
                }
                break;

            case 2:
                ethPrice *= 1.20f;
                OnEthPriceChanged?.Invoke();
                currentMarketEvent = "ETH Rally [+20%]";
                LogUtil.Event.Log(
                    "ETH Rally",
                    "ETH Price +20%");
                break;

            case 3:
                if (Globals.FloatUtil.GreaterOrEqual(ethPrice * 0.80f, ethMinMarketPrice))
                {
                    ethPrice *= 0.80f;
                    OnEthPriceChanged?.Invoke();
                    currentMarketEvent = "ETH Correction [-20%]";
                    LogUtil.Event.Log(
                        "ETH Correction",
                        "ETH Price -20%"
                    );
                }
                break;

            case 4:
                btcPrice *= 1.05f;
                OnBtcPriceChanged?.Invoke();
                ethPrice *= 1.05f;
                OnEthPriceChanged?.Invoke();
                currentMarketEvent = "Market Optimism [+5%]";
                LogUtil.Event.Log(
                    "Market Optimism",
                    "BTC & ETH Price +5%"
                );
                break;

            case 5:
                if (Globals.FloatUtil.GreaterOrEqual(btcPrice * 0.5f, btcMinMarketPrice))
                {
                    btcPrice *= 0.5f;
                    OnBtcPriceChanged?.Invoke();
                    currentMarketEvent = "BTC Manipulation [-100%]";
                    LogUtil.Event.Log(
                        "BTC Manipulation",
                        "BTC Price -100%"
                    );
                }
                break;

            case 6:
                if (Globals.FloatUtil.GreaterOrEqual(ethPrice * 0.5f, ethMinMarketPrice))
                {
                    ethPrice *= 0.5f;
                    OnEthPriceChanged?.Invoke();
                    currentMarketEvent = "ETH Manipulation [-100%]";
                    LogUtil.Event.Log(
                        "ETH Manipulation",
                        "ETH Price -100%"
                    );
                }
                break;

            case 7:
                btcPrice *= 2.0f;
                OnBtcPriceChanged?.Invoke();
                currentMarketEvent = "BTC Insider Trading [+100%]";
                LogUtil.Event.Log(
                    "BTC Insider Trading",
                    "BTC Price +100%"
                );
                break;

            case 8:
                ethPrice *= 2.0f;
                OnEthPriceChanged?.Invoke();
                currentMarketEvent = "ETH Insider Trading [+100%]";
                LogUtil.Event.Log(
                    "ETH Insider Trading",
                    "ETH Price +100%"
                );
                break;
            default:
                currentMarketEvent = "No Event";
                break;
        }

        SaveData();
    }

    #endregion
}