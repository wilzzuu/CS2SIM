using UnityEngine;
using System.IO;
using System.Collections;

[System.Serializable]
public class Player
{
    public int balance;
    public long btcOwned;
    public long ethOwned;

    public Player(int initialBalance, long initialBtc, long initialEth)
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

    public void AddMoney(int amount)
    {
        player.balance += amount;

        LogUtil.Economy.LogEuro(
            "ADD_MONEY",
            amount,
            player.balance
        );

        SavePlayerData();
        OnBalanceChanged?.Invoke();
    }

    public void DeductMoney(int amount)
    {
        if (player.balance >= amount)
        {
            player.balance -= amount;

            LogUtil.Economy.LogEuro(
                "DEDUCT_MONEY",
                -amount,
                player.balance
            );

            SavePlayerData();
            OnBalanceChanged?.Invoke();
        }
    }

    public void SetMoney(int amount)
    {
        player.balance = amount;

        LogUtil.Economy.LogEuro(
            "SET_MONEY",
            amount,
            player.balance
        );

        SavePlayerData();
        OnBalanceChanged?.Invoke();
    }

    public void SetBtc(long amount)
    {
        player.btcOwned = amount;
        long euroAmount = amount * CryptoSystem.Instance.GetBtcPrice() / Globals.CRYPTO_SCALE;

        LogUtil.Economy.LogCrypto(
            "SET_CRYPTO",
            amount,
            "BTC",
            player.btcOwned,
            $"-{Globals.FormatCurrencyStr(euroAmount, Globals.EURO_DECIMAL)}€"
        );

        SavePlayerData();
        OnBtcChanged?.Invoke();
    }

    public void AddBtc(long amount)
    {
        player.btcOwned += amount;
        long euroAmount = amount * CryptoSystem.Instance.GetBtcPrice() / Globals.CRYPTO_SCALE;

        LogUtil.Economy.LogCrypto(
            "ADD_CRYPTO",
            amount,
            "BTC",
            player.btcOwned,
            $"-{Globals.FormatCurrencyStr(euroAmount, Globals.EURO_DECIMAL)}€"
        );

        SavePlayerData();
        OnBtcChanged?.Invoke();
    }

    public void DeductBtc(long amount)
    {
        if (player.btcOwned >= amount)
        {
            player.btcOwned -= amount;
            long euroAmount = amount * CryptoSystem.Instance.GetBtcPrice() / Globals.CRYPTO_SCALE;

            LogUtil.Economy.LogCrypto(
                "DEDUCT_CRYPTO",
                -amount,
                "BTC",
                player.btcOwned,
                $"+{Globals.FormatCurrencyStr(euroAmount, Globals.EURO_DECIMAL)}€"
            );

            SavePlayerData();
            OnBtcChanged?.Invoke();
        }
    }

    public void MineBtc(long amount)
    {
        player.btcOwned += amount;

        LogUtil.Economy.LogCrypto(
            "MINE_CRYPTO",
            amount,
            "BTC",
            player.btcOwned
        );

        SavePlayerData();
        OnBtcChanged?.Invoke();
    }

    public void SetEth(long amount)
    {
        player.ethOwned = amount;
        long euroAmount = amount * CryptoSystem.Instance.GetEthPrice() / Globals.CRYPTO_SCALE;

        LogUtil.Economy.LogCrypto(
            "SET_CRYPTO",
            amount,
            "ETH",
            player.ethOwned,
            $"-{Globals.FormatCurrencyStr(euroAmount, Globals.EURO_DECIMAL)}€"
        );

        SavePlayerData();
        OnEthChanged?.Invoke();
    }

    public void AddEth(long amount)
    {
        player.ethOwned += amount;

        long euroAmount = amount * CryptoSystem.Instance.GetEthPrice() / Globals.CRYPTO_SCALE;

        LogUtil.Economy.LogCrypto(
            "ADD_CRYPTO",
            amount,
            "ETH",
            player.ethOwned,
            $"-{Globals.FormatCurrencyStr(euroAmount, Globals.EURO_DECIMAL)}€"
        );

        SavePlayerData();
        OnEthChanged?.Invoke();
    }

    public void DeductEth(long amount)
    {
        if (player.ethOwned >= amount)
        {
            player.ethOwned -= amount;
            long euroAmount = amount * CryptoSystem.Instance.GetEthPrice() / Globals.CRYPTO_SCALE;

            LogUtil.Economy.LogCrypto(
                "DEDUCT_CRYPTO",
                -amount,
                "ETH",
                player.ethOwned,
                $"+{Globals.FormatCurrencyStr(euroAmount, Globals.EURO_DECIMAL)}€"
            );

            SavePlayerData();
            OnEthChanged?.Invoke();
        }
    }

    public void MineEth(long amount)
    {
        player.ethOwned += amount;

        LogUtil.Economy.LogCrypto(
            "MINE_CRYPTO",
            amount,
            "ETH",
            player.ethOwned
        );

        SavePlayerData();
        OnBtcChanged?.Invoke();
    }

    public int GetPlayerBalance()
    {
        return player.balance;
    }

    public long GetBtcOwned()
    {   
        return player.btcOwned;
    }

    public long GetEthOwned()
    {
        return player.ethOwned;
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
            player = new Player(200 * Globals.EURO_SCALE, 0L, 0L);
        }
    }

    public void ResetProgress()
    {
        player = new Player(200 * Globals.EURO_SCALE, 0L, 0L);
        SavePlayerData();
        InventoryManager.Instance.ClearInventory(true);
        CollectionManager.Instance.ClearCollection();
        CryptoSystem.Instance.ClearData(true);
        DailyRewardSystem.Instance.ClearData(true);
        GiftManager.Instance.ClearData(true);
    }
}

[System.Serializable]
public class PlayerData
{
    public int balance;
    public long btcOwned;
    public long ethOwned;

    public PlayerData(PlayerManager playerManager)
    {
        balance = playerManager.GetPlayerBalance();
        btcOwned = playerManager.GetBtcOwned();
        ethOwned = playerManager.GetEthOwned();
    }
}
