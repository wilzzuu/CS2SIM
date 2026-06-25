using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Stock", menuName = "New Stock", order = 51)]
[System.Serializable]
public class Stock : ScriptableObject
{
    public string stockID;          // Unique identifier
    public string stockName;        // Display name
    public float currentPrice;      // Current price per share
    public float priceChangeRate;   // Rate of change for price variations
    public List<float> priceHistory; // To track past prices for graphing
    public Sprite stockIcon;        // Icon for the catalog
}