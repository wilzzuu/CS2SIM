using System;
using System.Collections;
using UnityEngine;

public class CryptoSystem : MonoBehaviour
{
    public static CryptoSystem Instance { get; private set; }

    [Header("Market Prices")]
    public float btcPrice = 100000f;
    public float lastBtcPrice = 100000f;
    public float ethPrice = 5000f;
    public float lastEthPrice = 5000f;
    private float _marketEventChance = 10f;

    [Header("Mining")]
    public int maxBtcMiners = 100;
    public int btcMiners = 0;
    public int maxEthMiners = 100;
    public int ethMiners = 0;
    public float btcMinerPrice = 100000f;
    public float ethMinerPrice = 5000f;
    public float btcPerHour;
    public float ethPerHour;

    [Header("Price Updates")]

    private DateTime _nextMiningUpdate;
    private DateTime _nextMarketUpdate;
    private DateTime _nextMarketEvent;
    private DateTime _lastMiningUpdate;

    private const string SaveKey = "CryptoSave";

    public delegate void BtcPriceHandler();
    public event BtcPriceHandler OnBtcPriceChanged;
    public delegate void BtcMinerHandler();
    public event BtcPriceHandler OnBtcMinersChanged;
    public delegate void EthPriceHandler();
    public event EthPriceHandler OnEthPriceChanged;
    public delegate void EthMinerHandler();
    public event EthPriceHandler OnEthMinersChanged;
    
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
        ProcessOfflineProgress();
        StartCoroutine(UpdateTimer());
        StartCoroutine(UpdateMiners());
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
                _nextMarketEvent = DateTime.UtcNow.AddHours(1);
            }
        }
    }

    private IEnumerator UpdateMiners()
    {
        while (true)
        {
            TimeSpan remaining = _nextMiningUpdate - DateTime.UtcNow;

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
        
        if (btcMiners > 0)
        {
            PlayerManager.Instance.MineBtc(GetBtcPerHour() * (float)hoursPassed);
        }
        if (ethMiners > 0)
        {
            PlayerManager.Instance.MineEth(GetEthPerHour() * (float)hoursPassed);
        }
        
        _lastMiningUpdate = now;

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
                PlayerManager.Instance.DeductCurrency(btcMinerPrice);
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
            PlayerManager.Instance.AddCurrency(GetBtcPrice());
            GetBtcPerHour();
            OnBtcMinersChanged?.Invoke();
            SaveData();
        }  
    }

    public void AddEthMiningRig()
    {
        if (ethMiners < maxEthMiners)
        {
            float ethMinerPrice = GetEthPrice();
            if (PlayerManager.Instance.GetPlayerBalance() >= ethMinerPrice)
            {
                ethMiners++;
                PlayerManager.Instance.DeductCurrency(ethMinerPrice);
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
            PlayerManager.Instance.AddCurrency(GetEthPrice());
            GetEthPerHour();
            OnEthMinersChanged?.Invoke();
            SaveData();
        }
    }

    public float GetBtcPrice()
    {
        return btcPrice;
    }

    public float GetEthPrice()
    {
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

            btcPerHour = btcPerHour,
            ethPerHour = ethPerHour,

            btcMiners = btcMiners,
            ethMiners = ethMiners,

            lastMiningUpdate =
                _lastMiningUpdate.ToString("O"),
            nextMiningUpdate =
                _nextMiningUpdate.ToString("O"),
            nextMarketUpdate = 
                _nextMarketUpdate.ToString("O"),
            nextMarketEvent =
                _nextMarketEvent.ToString("O")
        };

        PlayerPrefs.SetString(
            SaveKey,
            JsonUtility.ToJson(data));

        PlayerPrefs.Save();
    }

    public void LoadData()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
        {
            _lastMiningUpdate = DateTime.UtcNow;
            _nextMiningUpdate = DateTime.UtcNow;
            _nextMarketUpdate = DateTime.UtcNow;
            _nextMarketEvent = DateTime.UtcNow;

            SaveData();

            return;
        }

        CryptoSaveData data =
            JsonUtility.FromJson<CryptoSaveData>(
                PlayerPrefs.GetString(SaveKey));

        btcPrice = data.btcPrice;
        ethPrice = data.ethPrice;

        lastBtcPrice = data.lastBtcPrice;
        lastEthPrice = data.lastEthPrice;

        btcPerHour = data.btcPerHour;
        ethPerHour = data.ethPerHour;

        btcMiners = data.btcMiners;
        ethMiners = data.ethMiners;

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
        int eventRoll = UnityEngine.Random.Range(0, 5);

        switch (eventRoll)
        {
            case 0:
                btcPrice *= 1.15f;
                LogUtil.Event.Log(
                    "BTC Bull Run",
                    "BTC Price +15%");
                break;

            case 1:
                btcPrice *= 0.85f;
                LogUtil.Event.Log(
                    "BTC Crash",
                    "BTC Price -15%");
                break;

            case 2:
                ethPrice *= 1.20f;
                LogUtil.Event.Log(
                    "ETH Rally",
                    "ETH Price +20%");
                break;

            case 3:
                ethPrice *= 0.80f;
                LogUtil.Event.Log(
                    "ETH Correction",
                    "ETH Price -20%");
                break;

            case 4:
                btcPrice *= 1.05f;
                ethPrice *= 1.05f;
                LogUtil.Event.Log(
                    "Market Optimism",
                    "BTC & ETH Price +5%"
                    );
                break;
        }

        SaveData();
    }

    #endregion
}