using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Windows.Speech;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private static string SaveFilePath
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

    private static string SaveFileName = "save.cs2sim.json";

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
    }

    public static class Keys
    {
        public const string All = "All";
        public const string Collection = "Collection";
        public const string Crypto = "Crypto";
        public const string DailyReward = "DailyReward";
        public const string Gift = "Gift";
        public const string Inventory = "Inventory";
        public const string Player = "Player";
    }

    [Serializable]
    public class GameSaveData
    {
        public CollectionSaveData Collection;
        public CryptoSaveData Crypto;
        public DailyRewardSaveData DailyReward;
        public GiftSaveData Gift;
        public InventorySaveData Inventory;
        public PlayerSaveData Player;
    }

    [Serializable]
    public class CollectionSaveData
    {
        
    }

    [Serializable]
    public class CryptoSaveData
    {
        
    }

    [Serializable]
    public class DailyRewardSaveData
    {
        
    }

    [Serializable]
    public class GiftSaveData
    {
        
    }

    [Serializable]
    public class InventorySaveData
    {
        
    }

    [Serializable]
    public class PlayerSaveData
    {
        
    }
}