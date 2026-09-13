using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Stock", menuName = "New Stock", order = 51)]
[System.Serializable]
public class Stock : ScriptableObject
{
    public string stockID;          // Unique identifier
    public string stockName;        // Display name
    public int currentPrice;        // Current price per share (cents)
    public int priceChangeRate;     // Max random fluctuation per tick (cents)
    public List<int> priceHistory; // To track past prices for graphing
    public Sprite stockIcon;        // Icon for the catalog
}