using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Item Data", order = 51)]
public class ItemData : ScriptableObject
{
    public string ID;
    public string Name;
    public string Rarity;
    public float BasePrice;
    public float Price;
    public int DemandScore;
    public int Weight;
    public string Color;
    public string Item;
    public string Style;

    public float LastActivityTime;
    public const float DemandDecayInterval = 120f;
    public const float DecayRate = 0.1f;

    private void OnEnable()
    {
        DemandScore = 0;
        Price = BasePrice;
        LastActivityTime = Time.time;
    }
}
