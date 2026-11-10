using UnityEngine;
using System.Collections;

public class AutoCaseOpener : MonoBehaviour
{
    public CaseOpening caseOpeningSystem;
    public int numberOfOpens = 1;
    public float delayBetweenOpens = 2f;
    
    private bool _isAutoOpening;

    private void OnValidate()
    {
        if (caseOpeningSystem == null)
        {
            caseOpeningSystem = FindAnyObjectByType<CaseOpening>();
        }
    }

    public void StartAutoOpening()
    {
        if (_isAutoOpening)
        {
            Debug.LogWarning("Auto-opening already in progress.");
            return;
        }

        if (caseOpeningSystem == null)
        {
            Debug.LogError("CaseOpening system not assigned.");
            return;
        }

        StartCoroutine(AutoOpenCoroutine());
    }

    public void StopAutoOpening()
    {
        _isAutoOpening = false;
    }

    private IEnumerator AutoOpenCoroutine()
    {
        _isAutoOpening = true;

        for (int i = 0; i < numberOfOpens; i++)
        {
            caseOpeningSystem.OpenCase();
            
            if (i < numberOfOpens - 1)
            {
                yield return new WaitForSeconds(delayBetweenOpens);
            }
        }

        _isAutoOpening = false;
    }
}
