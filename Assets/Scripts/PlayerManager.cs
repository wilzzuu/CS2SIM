using UnityEngine;
using System.IO;
using System.Collections;

[System.Serializable]
public class Player : MonoBehaviour
{
    public float balance;
    public float btcOwned;
    public float ethOwned;

    public Player(float initialBalance, float initialBtc, float initialEth)
    {
        balance = initialBalance;
        btcOwned = initialBtc;
        ethOwned = initialEth;
    }
}

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }
    public Player player;

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
    
    private const string SaveFileName = "PlayerData.save";

    public delegate void BalanceChangedHandler();
    public event BalanceChangedHandler OnBalanceChanged;
    public delegate void BtcChangedHandler();
    public event BtcChangedHandler OnBtcChanged;
    public delegate void EthChangedHandler();
    public event EthChangedHandler OnEthChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            StartCoroutine(InitializePlayerAndInventory());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private IEnumerator InitializePlayerAndInventory()
    {
        while (InventoryManager.Instance == null)
        {
            yield return null;
        }

        CheckPlayerSave();
    }

    public void AddCurrency(float amount)
    {
        player.balance = Globals.FloatUtil.Round(player.balance + amount);

        LogUtil.Economy.LogEuro(
            "ADD_CURRENCY",
            amount,
            player.balance
        );

        SavePlayerData();
        OnBalanceChanged?.Invoke();
    }

    public void DeductCurrency(float amount)
    {
        if (Globals.FloatUtil.GreaterOrEqual(player.balance, amount))
        {
            player.balance = Globals.FloatUtil.Round(player.balance - amount);

            LogUtil.Economy.LogEuro(
                "DEDUCT_CURRENCY",
                -amount,
                player.balance
            );

            SavePlayerData();
            OnBalanceChanged?.Invoke();
        }
    }

    public void AddBtc(float amount)
    {
        player.btcOwned = Globals.FloatUtil.RoundCrypto(player.btcOwned + amount);
        float euroAmount = amount * CryptoSystem.Instance.GetBtcPrice();

        LogUtil.Economy.LogCrypto(
            "ADD_CRYPTO",
            amount,
            "BTC",
            player.btcOwned,
            $"-{euroAmount:F2}€"
        );

        SavePlayerData();
        OnBtcChanged?.Invoke();
    }

    public void DeductBtc(float amount)
    {
        if (Globals.FloatUtil.GreaterOrEqual(player.btcOwned, amount))
        {
            player.btcOwned = Globals.FloatUtil.RoundCrypto(player.btcOwned - amount);
            float euroAmount = amount * CryptoSystem.Instance.GetBtcPrice();

            LogUtil.Economy.LogCrypto(
                "DEDUCT_CRYPTO",
                -amount,
                "BTC",
                player.btcOwned,
                $"+{euroAmount:F2}€"
            );

            SavePlayerData();
            OnBtcChanged?.Invoke();
        }
    }

    public void MineBtc(float amount)
    {
        player.btcOwned = Globals.FloatUtil.RoundCrypto(player.btcOwned + amount);
        float euroAmount = amount * CryptoSystem.Instance.GetBtcPrice();

        LogUtil.Economy.LogCrypto(
            "MINE_CRYPTO",
            amount,
            "BTC",
            player.btcOwned
        );

        SavePlayerData();
        OnBtcChanged?.Invoke();
    }

    public void AddEth(float amount)
    {
        player.ethOwned = Globals.FloatUtil.RoundCrypto(player.ethOwned + amount);

        float euroAmount = amount * CryptoSystem.Instance.GetEthPrice();

        LogUtil.Economy.LogCrypto(
            "ADD_CRYPTO",
            amount,
            "ETH",
            player.ethOwned,
            $"-{euroAmount:F2}€"
        );

        SavePlayerData();
        OnEthChanged?.Invoke();
    }

    public void DeductEth(float amount)
    {
        if (Globals.FloatUtil.GreaterOrEqual(player.ethOwned, amount))
        {
            player.ethOwned = Globals.FloatUtil.RoundCrypto(player.ethOwned - amount);
            float euroAmount = amount * CryptoSystem.Instance.GetEthPrice();

            LogUtil.Economy.LogCrypto(
                "DEDUCT_CRYPTO",
                -amount,
                "ETH",
                player.ethOwned,
                $"+{euroAmount:F2}€"
            );

            SavePlayerData();
            OnEthChanged?.Invoke();
        }
    }

    public void MineEth(float amount)
    {
        player.ethOwned = Globals.FloatUtil.RoundCrypto(player.ethOwned + amount);
        float euroAmount = amount * CryptoSystem.Instance.GetEthPrice();

        LogUtil.Economy.LogCrypto(
            "MINE_CRYPTO",
            amount,
            "ETH",
            player.ethOwned
        );

        SavePlayerData();
        OnBtcChanged?.Invoke();
    }

    public float GetPlayerBalance()
    {
        return Globals.FloatUtil.Round(player.balance);
    }

    public float GetBtcOwned()
    {   
        return Globals.FloatUtil.RoundCrypto(player.btcOwned);
    }

    public float GetEthOwned()
    {
        return Globals.FloatUtil.RoundCrypto(player.ethOwned);
    }

    void CheckPlayerSave()
    {
        string path = Path.Combine(SaveFilePath, SaveFileName);

        if (!File.Exists(path))
        {
            Debug.LogWarning("Player save file missing or tampered with. Clearing inventory as a security measure.");
            InventoryManager.Instance.ClearInventory(true);

            ResetProgress();
        }
        else 
        {
            LoadPlayerData();
        }
    }

    public void SavePlayerData()
    {
        string path = Path.Combine(SaveFilePath, SaveFileName);
        string jsonData = JsonUtility.ToJson(player);
        string encryptedData = DataEncryptionUtility.Encrypt(jsonData);

        File.WriteAllText(path, encryptedData);
    }

    public void LoadPlayerData()
    {
        string path = Path.Combine(SaveFilePath, SaveFileName);

        if (File.Exists(path))
        {
            string encryptedData = File.ReadAllText(path);
            string jsonData = DataEncryptionUtility.Decrypt(encryptedData);

            player = JsonUtility.FromJson<Player>(jsonData);
        }
        else
        {
            player = new Player(200f, 0f, 0f);
        }
    }

    public void ResetProgress()
    {
        player = new Player(200f, 0f, 0f);
        SavePlayerData();
        InventoryManager.Instance.ClearInventory(true);
        CollectionManager.Instance.ClearCollection();
        CryptoSystem.Instance.ClearData();
        
        DailyRewardManager dailyRewardManager = new DailyRewardManager();
        dailyRewardManager.ClearData();
    }
}

[System.Serializable]
public class PlayerData
{
    public float balance;
    public float btcOwned;
    public float ethOwned;

    public PlayerData(PlayerManager playerManager)
    {
        balance = playerManager.GetPlayerBalance();
        btcOwned = playerManager.GetBtcOwned();
        ethOwned = playerManager.GetEthOwned();
    }
}
