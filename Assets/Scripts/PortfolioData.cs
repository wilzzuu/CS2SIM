using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PortfolioData", menuName = "Scriptable Objects/PortfolioData")]
[System.Serializable]
public class PortfolioData : ScriptableObject
{
    public List<PortfolioEntry> entries;
}
