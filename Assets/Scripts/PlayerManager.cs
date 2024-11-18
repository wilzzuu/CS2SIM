using System;
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

    private string SaveFileName
    {
        get
        {
            #if UNITY_EDITOR
                return "PlayTest_PlayerData.dat";
            #else
                return "PlayerData.dat";
            #endif
        }
    }

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
        OnBalanceChanged?.Invoke();
        SavePlayerData();
    }

    public void DeductCurrency(float amount)
    {
        if (player.balance >= amount)
        {
            player.balance -= amount;
            OnBalanceChanged?.Invoke();
            SavePlayerData();
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
        BinaryFormatter bf = new BinaryFormatter();
        string path = Path.Combine(SaveFilePath, SaveFileName);
        using (FileStream file = File.Create(path))
        {
            bf.Serialize(file, player);
        }
    }

    public void LoadPlayerData()
    {
        string path = Path.Combine(SaveFilePath, SaveFileName);

        if (File.Exists(path))
        {
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                using (FileStream file = File.Open(path, FileMode.Open))
                {
                    player = (Player)bf.Deserialize(file);
                }

                // Validation: Check if the balance is within a reasonable range
                if (player.balance < 0 || player.balance > 1_000_000)
                {
                    throw new Exception("Corrupted data detected.");
                }

                Debug.Log($"Player data loaded successfully. Balance: {player.balance}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load save file. Reason: {ex.Message}");
                ResetProgress(); // Reset to default values
            }
        }
        else
        {
            Debug.LogWarning("Save file not found. Creating new player data.");
            ResetProgress();
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
