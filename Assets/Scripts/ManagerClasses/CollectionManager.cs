using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class CollectionData
{
    public List<string> collectedItemIDs;
}

public class CollectionManager : MonoBehaviour
{
    public static CollectionManager Instance { get; private set; }

    public GameObject collectionItemPrefab;
    public Transform collectionGrid;

    private List<ItemData> _allItems = new List<ItemData>();
    private HashSet<string> _collectedItemIDs = new HashSet<string>();

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
    
    private const string SaveFileName = "CollectionData.save";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        LoadCollection();
    }

    void Start()
    {
        LoadAllGameItems();
        UpdateCollectionUI();
    }

    public void AddItemToCollection(ItemData item)
    {
        if (_collectedItemIDs.Add(item.id))
        {
            SaveCollection();
            UpdateCollectionUI();
        }
    }

    private void SaveCollection()
    {
        CollectionData data = new CollectionData
        {
            collectedItemIDs = new List<string>(_collectedItemIDs)
        };

        string path = Path.Combine(SaveFilePath, SaveFileName);
        string jsonData = JsonUtility.ToJson(data);
        string encryptedData = DataEncryptionUtility.Encrypt(jsonData);

        File.WriteAllText(path, encryptedData);
    }

    private void LoadCollection()
    {
        string path = Path.Combine(SaveFilePath, SaveFileName);

        if (File.Exists(path))
        {
            string encryptedData = File.ReadAllText(path);
            string jsonData = DataEncryptionUtility.Decrypt(encryptedData);

            CollectionData data = JsonUtility.FromJson<CollectionData>(jsonData);
            _collectedItemIDs = new HashSet<string>(data.collectedItemIDs);
        }
    }

    private List<ItemData> LoadAllGameItems()
    {
        _allItems = Resources.LoadAll<ItemData>("ItemAssets").ToList();
        return _allItems;
    }

    public void ClearCollection()
    {
        _collectedItemIDs.Clear();
        string path = Path.Combine(SaveFilePath, SaveFileName);

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        SaveCollection();
    }

    public void UpdateCollectionUI()
    {
        if (collectionGrid == null) return;

        foreach (Transform child in collectionGrid)
        {
            Destroy(child.gameObject);
        }

        foreach (var item in LoadAllGameItems())
        {
            GameObject itemObj = Instantiate(collectionItemPrefab, collectionGrid);
            CollectionItem collectionItem = itemObj.GetComponent<CollectionItem>();

            bool isCollected = _collectedItemIDs.Contains(item.id);
            collectionItem.Setup(item, isCollected);
        }
    }
}
