using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Item Data", order = 51)]
public class ItemData : ScriptableObject
{
    public string id;
    public string gun;
    public new string name;
    public float basePrice;
    public float price;
    public string condition;
    public bool isStatTrak;
    public string type;
    public string rarity;
    public int weight;
    public int demandScore;

    public float lastActivityTime;
    public const float DemandDecayInterval = 120f;
    public const float DecayRate = 0.1f;

    private void OnEnable()
    {
        demandScore = 0;
        price = basePrice;
        lastActivityTime = Time.time;
    }
}
