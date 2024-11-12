using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New CrateData", menuName = "Case Data", order = 51)]
public class CaseData : ScriptableObject
{
    public string ID;
    public string Name;
    public float Price;
    public List<ItemData> Items;
}
