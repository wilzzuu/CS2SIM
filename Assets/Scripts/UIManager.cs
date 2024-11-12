using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI balance;

    [SerializeField] private List<Button> buttonList = new List<Button>();

    private bool _isUILocked = false;

    void Start()
    {
        if (PlayerManager.Instance == null)
        {
            Debug.LogError("PlayerManager does not exist in the current context.");
            return;
        }
        PlayerManager.Instance.OnBalanceChanged += UpdateBalanceDisplay;

        UpdateBalanceDisplay();
    }

    void OnDestroy()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnBalanceChanged -= UpdateBalanceDisplay;
        }
    }
    
    void UpdateBalanceDisplay()
    {
        float balancePreformat = PlayerManager.Instance.GetPlayerBalance();
        string formattedBalance = balancePreformat.ToString("F2");
        balance.text = formattedBalance;
    }

    public void LockButton(Button button)
    {
        button.interactable = false;   
    }

    public void UnlockButton(Button button)
    {
        button.interactable = true;   
    }

    public void LockUI()
    {
        if (!_isUILocked)
        {
            foreach (Button button in buttonList)
            {
                LockButton(button);
            }
            _isUILocked = true;
        }
    }

    public void UnlockUI()
    {
        if (_isUILocked)
        {
            foreach (Button button in buttonList)
            {
                UnlockButton(button);
            }
        }
        _isUILocked = false;
    }
}
