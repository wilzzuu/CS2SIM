using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "New CaseData", menuName = "Case Data", order = 51)]
public class CaseData : ScriptableObject
{
    public string id;
    public new string name;
    public float price;
    public List<ItemData> items;
}
