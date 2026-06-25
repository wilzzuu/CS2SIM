using UnityEngine;
using System.Collections.Generic;

public class CaseManager : MonoBehaviour
{
    public List<CaseData> allCases;

    public CaseData GetCaseByID(string caseID)
    {
        foreach (CaseData caseData in allCases)
        {
            if (caseData.id == caseID)
            {
                return caseData;
            }
        }
        Debug.LogError("Case not found: " + caseID);
        return null;
    }

    public ItemData GetItemByID(string itemID)
    {
        foreach (CaseData caseData in allCases)
        {
            foreach (ItemData item in caseData.items)
            {
                if (item.id == itemID)
                {
                    return item;
                }
            }
        }
        Debug.LogError("Item not found: " + itemID);
        return null;
    }
}
