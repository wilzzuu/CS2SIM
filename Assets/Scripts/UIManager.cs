using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class UIManager : MonoBehaviour
{
    public GameObject buttonsContainer;
    public TextMeshProUGUI balance;
    public TextMeshProUGUI inventoryValueText;

    public TextMeshProUGUI btcValueText;
    public TextMeshProUGUI btcAmountText;
    public TextMeshProUGUI btcDiffText;
    public TextMeshProUGUI btcPriceText;
    public TextMeshProUGUI btcPerHourText;
    public TextMeshProUGUI btcEurPerHourText;

    public TextMeshProUGUI ethValueText;
    public TextMeshProUGUI ethAmountText;
    public TextMeshProUGUI ethDiffText;
    public TextMeshProUGUI ethPriceText;
    public TextMeshProUGUI ethPerHourText;
    public TextMeshProUGUI ethEurPerHourText;

    public enum SidebarPage
    {
        Inventory,
        Market,
        CaseOpening,
        GameSelector,
        Crypto,
        Collection,
        Achievements,
        Rewards,
        DailyRewards,
        Notifications,
        Settings
    }

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
        PlayerManager.Instance.OnBtcChanged += UpdateBitcoinDisplay;
        PlayerManager.Instance.OnEthChanged += UpdateEthereumDisplay;
        InventoryManager.Instance.OnInventoryValueChanged += UpdateInventoryValueDisplay;
        CryptoSystem.Instance.OnBtcPriceChanged += UpdateBitcoinDisplay;
        CryptoSystem.Instance.OnEthPriceChanged += UpdateEthereumDisplay;
        CryptoSystem.Instance.OnBtcMinersChanged += UpdateBitcoinDisplay;
        CryptoSystem.Instance.OnEthMinersChanged += UpdateEthereumDisplay;

        UpdateBalanceDisplay();
        UpdateInventoryValueDisplay();
        UpdateBitcoinDisplay();
        UpdateEthereumDisplay();
    }

    void OnDestroy()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnBalanceChanged -= UpdateBalanceDisplay;
            PlayerManager.Instance.OnBtcChanged -= UpdateBitcoinDisplay;
            PlayerManager.Instance.OnEthChanged -= UpdateEthereumDisplay;
        }
        
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryValueChanged -= UpdateInventoryValueDisplay;
        }

        if (CryptoSystem.Instance != null)
        {
            CryptoSystem.Instance.OnBtcPriceChanged -= UpdateBitcoinDisplay;
            CryptoSystem.Instance.OnEthPriceChanged -= UpdateEthereumDisplay;
        }
    }
    
    void UpdateBalanceDisplay()
    {
        float bal = PlayerManager.Instance.GetPlayerBalance();
        balance.text = $"{bal:F2}€";
    }

    void UpdateBitcoinDisplay()
    {
        float btc = PlayerManager.Instance.GetBtcOwned();
        float btcValue = CryptoSystem.Instance.GetBtcPrice();
        float btcWallet = btc * btcValue;

        float btcPriceDiff = btcValue - CryptoSystem.Instance.lastBtcPrice;
        bool isBtcNegative = Single.IsNegative(btcPriceDiff);

        btcPriceText.text = $"{btcValue:F2}€";
        btcDiffText.text = $"{(isBtcNegative ? "" : "+")}{btcPriceDiff:F2}€";
        btcDiffText.color = isBtcNegative ? Color.red : Color.green;

        if (btcWallet > 0.01f)
        {
            btcAmountText.text = $"{btc:F8}";
            btcValueText.text = $"{btcWallet:F2}€";
        }
        else
        {
            btcAmountText.text = $"{0:F8}";
            btcValueText.text = $"{0:F2}€";
        }

        int btcMiners = CryptoSystem.Instance.GetBtcMinerCount();
        float btcPerHour = CryptoSystem.Instance.GetBtcPerHour();
        float eurPerHour = btcValue * btcPerHour;

        if (btcMiners > 0)
        {
            btcPerHourText.text = $"{btcPerHour:F8}/h";
            btcEurPerHourText.text = $"{eurPerHour:F2}€/h";
        }
        else
        {
            btcPerHourText.text = $"{0}/h";
            btcEurPerHourText.text = $"{0:F2}€/h";
        }
    }

    void UpdateEthereumDisplay()
    {
        float eth = PlayerManager.Instance.GetEthOwned();
        float ethValue = CryptoSystem.Instance.GetEthPrice();
        float ethWallet = eth * ethValue;

        float ethPriceDiff = ethValue - CryptoSystem.Instance.lastEthPrice;
        bool isEthNegative = Single.IsNegative(ethPriceDiff);

        ethPriceText.text = $"{ethValue:F2}€";
        ethDiffText.text = $"{(isEthNegative ? "" : "+")}{ethPriceDiff:F2}€";
        ethDiffText.color = isEthNegative ? Color.red : Color.green;

        if (ethWallet > 0.01f)
        {
            ethAmountText.text = $"{eth:F8}";
            ethValueText.text = $"{ethWallet:F2}€";
        }
        else
        {
            ethAmountText.text = $"{0:F8}";
            ethValueText.text = $"{0:F2}€";
        }

        int ethMiners = CryptoSystem.Instance.GetEthMinerCount();
        float ethPerHour = CryptoSystem.Instance.GetEthPerHour();
        float eurPerHour = ethValue * ethPerHour;

        if (ethMiners > 0)
        {
            ethPerHourText.text = $"{ethPerHour:F8}/h";
            ethEurPerHourText.text = $"{eurPerHour:F2}€/h";
        }
        else
        {
            ethPerHourText.text = $"{0}/h";
            ethEurPerHourText.text = $"{0:F2}€/h";
        }
    }

    void UpdateInventoryValueDisplay()
    {
        float inventoryValue = InventoryManager.Instance.CalculateInventoryValue();
        inventoryValueText.text = $"{inventoryValue:F2}€";
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
