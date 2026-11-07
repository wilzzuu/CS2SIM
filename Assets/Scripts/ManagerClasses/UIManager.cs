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
        ConfigureResponsiveLayout();

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

    private void ConfigureResponsiveLayout()
    {
        RectTransform root = GetComponent<RectTransform>();
        Transform sidebarTransform = transform.Find("SideBar");
        RectTransform sidebar = sidebarTransform as RectTransform;
        Transform backgroundTransform = sidebarTransform != null ? sidebarTransform.Find("Background") : null;
        RectTransform sidebarBackground = backgroundTransform as RectTransform;
        RectTransform navigation = buttonsContainer != null ? buttonsContainer.GetComponent<RectTransform>() : null;
        Image headerImage = GetComponent<Image>();
        Image sidebarImage = backgroundTransform != null ? backgroundTransform.GetComponent<Image>() : null;

        if (root == null || sidebar == null || sidebarBackground == null || navigation == null ||
            headerImage == null || sidebarImage == null)
        {
            Debug.LogError("The shared UI is missing a required responsive layout component.", this);
            return;
        }

        root.anchorMin = new Vector2(0f, root.anchorMin.y);
        root.anchorMax = new Vector2(1f, root.anchorMax.y);
        root.anchoredPosition = new Vector2(40f, root.anchoredPosition.y);
        root.sizeDelta = new Vector2(-80f, root.sizeDelta.y);

        sidebar.anchorMin = new Vector2(0f, sidebar.anchorMin.y);
        sidebar.anchorMax = new Vector2(0f, sidebar.anchorMax.y);
        sidebar.anchoredPosition = new Vector2(-40f, sidebar.anchoredPosition.y);
        sidebarBackground.anchoredPosition = new Vector2(0f, sidebarBackground.anchoredPosition.y);
        navigation.anchoredPosition = new Vector2(10f, navigation.anchoredPosition.y);

        headerImage.color = new Color32(16, 27, 43, 255);
        sidebarImage.color = new Color32(11, 20, 33, 255);

        foreach (Button button in buttonsContainer.GetComponentsInChildren<Button>(true))
        {
            ColorBlock colors = button.colors;
            colors.normalColor = new Color32(29, 43, 64, 255);
            colors.highlightedColor = new Color32(53, 211, 192, 255);
            colors.pressedColor = new Color32(24, 151, 145, 255);
            colors.selectedColor = new Color32(42, 180, 167, 255);
            colors.disabledColor = new Color32(60, 71, 89, 160);
            button.colors = colors;
        }
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
        int bal = PlayerManager.Instance.GetPlayerBalance();
        balance.text = $"{Globals.FormatCurrencyStr(bal, Globals.EURO_DECIMAL)}€";
    }

    void UpdateBitcoinDisplay()
    {
        long btc = PlayerManager.Instance.GetBtcOwned();
        int btcValue = CryptoSystem.Instance.GetBtcPrice();
        long btcWallet = btc * btcValue / Globals.CRYPTO_SCALE;

        int btcPriceDiff = btcValue - CryptoSystem.Instance.lastBtcPrice;
        bool isBtcNegative = btcPriceDiff < 0;

        btcPriceText.text = $"{Globals.FormatCurrencyStr(btcValue, Globals.EURO_DECIMAL)}€";
        btcDiffText.text = $"{(isBtcNegative ? "" : "+")}{Globals.FormatCurrencyStr(btcPriceDiff, Globals.EURO_DECIMAL)}€";
        btcDiffText.color = isBtcNegative ? Color.red : Color.green;

        if (btcWallet > 0)
        {
            btcAmountText.text = $"{Globals.FormatCurrencyStr(btc, Globals.CRYPTO_DECIMAL)}";
            btcValueText.text = $"{Globals.FormatCurrencyStr(btcWallet, Globals.EURO_DECIMAL)}€";
        }
        else
        {
            btcAmountText.text = $"{Globals.FormatCurrencyStr(0, Globals.CRYPTO_DECIMAL)}";
            btcValueText.text = $"{Globals.FormatCurrencyStr(0, Globals.EURO_DECIMAL)}€";
        }

        int btcMiners = CryptoSystem.Instance.GetBtcMinerCount();
        long btcPerHour = CryptoSystem.Instance.GetBtcPerHour();
        long eurPerHour = (long)btcValue * btcPerHour / Globals.CRYPTO_SCALE;

        if (btcMiners > 0)
        {
            btcPerHourText.text = $"{Globals.FormatCurrencyStr(btcPerHour, Globals.CRYPTO_DECIMAL)}/h";
            btcEurPerHourText.text = $"{Globals.FormatCurrencyStr(eurPerHour, Globals.EURO_DECIMAL)}€/h";
        }
        else
        {
            btcPerHourText.text = $"{Globals.FormatCurrencyStr(0, Globals.CRYPTO_DECIMAL)}/h";
            btcEurPerHourText.text = $"{Globals.FormatCurrencyStr(0, Globals.EURO_DECIMAL)}€/h";
        }
    }

    void UpdateEthereumDisplay()
    {
        long eth = PlayerManager.Instance.GetEthOwned();
        int ethValue = CryptoSystem.Instance.GetEthPrice();
        long ethWallet = eth * ethValue / Globals.CRYPTO_SCALE;

        int ethPriceDiff = ethValue - CryptoSystem.Instance.lastEthPrice;
        bool isEthNegative = ethPriceDiff < 0;

        ethPriceText.text = $"{Globals.FormatCurrencyStr(ethValue, Globals.EURO_DECIMAL)}€";
        ethDiffText.text = $"{(isEthNegative ? "" : "+")}{Globals.FormatCurrencyStr(ethPriceDiff, Globals.EURO_DECIMAL)}€";
        ethDiffText.color = isEthNegative ? Color.red : Color.green;

        if (ethWallet > 0)
        {
            ethAmountText.text = $"{Globals.FormatCurrencyStr(eth, Globals.CRYPTO_DECIMAL)}";
            ethValueText.text = $"{Globals.FormatCurrencyStr(ethWallet, Globals.EURO_DECIMAL)}€";
        }
        else
        {
            ethAmountText.text = $"{Globals.FormatCurrencyStr(0, Globals.CRYPTO_DECIMAL)}";
            ethValueText.text = $"{Globals.FormatCurrencyStr(0, Globals.EURO_DECIMAL)}€";
        }

        int ethMiners = CryptoSystem.Instance.GetEthMinerCount();
        long ethPerHour = CryptoSystem.Instance.GetEthPerHour();
        long eurPerHour = (long)ethValue * ethPerHour / Globals.CRYPTO_SCALE;

        if (ethMiners > 0)
        {
            ethPerHourText.text = $"{Globals.FormatCurrencyStr(ethPerHour, Globals.CRYPTO_DECIMAL)}/h";
            ethEurPerHourText.text = $"{Globals.FormatCurrencyStr(eurPerHour, Globals.EURO_DECIMAL)}€/h";
        }
        else
        {
            ethPerHourText.text = $"{Globals.FormatCurrencyStr(0, Globals.CRYPTO_DECIMAL)}/h";
            ethEurPerHourText.text = $"{Globals.FormatCurrencyStr(0, Globals.EURO_DECIMAL)}€/h";
        }
    }

    void UpdateInventoryValueDisplay()
    {
        int inventoryValue = InventoryManager.Instance.CalculateInventoryValue();
        inventoryValueText.text = $"{Globals.FormatCurrencyStr(inventoryValue, Globals.EURO_DECIMAL)}€";
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
