using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using System.Linq;

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
    private string SaveFileName
    {
        get
        {
            #if UNITY_EDITOR
                return "PlayTest_CollectionData.dat";
            #else
                return "CollectionData.dat";
            #endif
        }
    }

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

    // ReSharper disable Unity.PerformanceAnalysis
    public void AddItemToCollection(ItemData item)
    {
        if (_collectedItemIDs.Contains(item.id)) return;

        _collectedItemIDs.Add(item.id);
        SaveCollection();
        UpdateCollectionUI();
    }

    private void SaveCollection()
    {   
        BinaryFormatter bf = new BinaryFormatter();
        string path = Path.Combine(SaveFilePath, SaveFileName);
        using (FileStream file = File.Create(path))
        {
            bf.Serialize(file, _collectedItemIDs);
        }
    }

    private void LoadCollection()
    {
        string path = Path.Combine(SaveFilePath, SaveFileName);
        if (File.Exists(path))
        {
            using (FileStream file = File.Open(path, FileMode.Open))
            {
                BinaryFormatter bf = new BinaryFormatter();
                _collectedItemIDs = (HashSet<string>)bf.Deserialize(file);
            }
        }
        UpdateCollectionUI();
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
