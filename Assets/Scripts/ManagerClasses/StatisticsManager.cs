using UnityEngine;

public class StatisticsManager : MonoBehaviour
{
    public static StatisticsManager Instance;

    public int totalCasesOpened;
    public int totalItemsSold;
    public int totalItemsBought;

    public float totalCryptoMined;
    public float totalCryptoProfit;

    public int totalSlotWins;
    public int totalCrashWins;

    private void Awake()
    {
        Instance = this;
    }
}