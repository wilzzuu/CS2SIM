using UnityEngine;

[CreateAssetMenu(fileName = "Portfolio Entry", menuName = "New Portfolio Entry", order = 51)]
[System.Serializable]
public class PortfolioEntry : ScriptableObject
{
    public string stockID;
    public int quantityOwned;
}