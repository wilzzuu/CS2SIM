using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;

[System.Serializable]
public class Player
{
    public float balance;

    public Player(float initialBalance)
    {
        balance = initialBalance;
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
        player.balance += amount;
        SavePlayerData();
        OnBalanceChanged?.Invoke();
    }

    public void DeductCurrency(float amount)
    {
        if (player.balance >= amount)
        {
            player.balance -= amount;
            SavePlayerData();
            OnBalanceChanged?.Invoke();
        }
    }

    public float GetPlayerBalance()
    {
        return player.balance;
    }

    void CheckPlayerSave()
    {
        string path = Path.Combine(SaveFilePath, SaveFileName);

        if (!File.Exists(path))
        {
            Debug.LogWarning("Player save file missing or tampered with. Clearing inventory as a security measure.");
            InventoryManager.Instance.ClearInventory();

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
            player = new Player(200f);
        }
    }

    public void ResetProgress()
    {
        player = new Player(200f);
        SavePlayerData();
        InventoryManager.Instance.ClearInventory();
        CollectionManager.Instance.ClearCollection();
    }
}

[System.Serializable]
public class PlayerData
{
    public float balance;

    public PlayerData(PlayerManager playerManager)
    {
        balance = playerManager.GetPlayerBalance();
    }
}
