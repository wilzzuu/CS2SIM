using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CryptoManager : MonoBehaviour
{
    public Button showExchangeTab;
    public Button showMiningTab;
    public Button showBtcTab;
    public Button showEthTab;
    public GameObject exchangeTabObject;
    public GameObject miningTabObject;
    private bool _isExchangeTab = true;
    private bool _isBtcTab = true;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI perHourText;
    public TextMeshProUGUI minerAmountText;
    public TextMeshProUGUI minerPriceText;
    public Image bigIcon;
    public TextMeshProUGUI buySectionText;
    public TextMeshProUGUI sellSectionText;
    public TMP_InputField euroAmountInput;
    public TextMeshProUGUI euroAmountPlaceholder;
    public TMP_InputField cryptoAmountInput;
    public TextMeshProUGUI cryptoAmountPlaceholder;
    public TextMeshProUGUI cryptoOutputText;
    public TextMeshProUGUI euroOutputText;
    public TextMeshProUGUI priceDiffText;
    public TextMeshProUGUI marketEventText;

    void Start()
    {
        CryptoSystem.Instance.OnBtcPriceChanged += UpdateBitcoinPriceDisplay;
        CryptoSystem.Instance.OnEthPriceChanged += UpdateEthereumPriceDisplay;
        ShowExchangeTab();
    }

    void OnDestroy()
    {
        if (CryptoSystem.Instance != null)
        {
            CryptoSystem.Instance.OnBtcPriceChanged += UpdateBitcoinPriceDisplay;
            CryptoSystem.Instance.OnEthPriceChanged += UpdateEthereumPriceDisplay;
        }
    }

    void UpdateBitcoinPriceDisplay()
    {
        if (_isBtcTab)
        {
            float _btcPrice = CryptoSystem.Instance.GetBtcPrice();
            priceText.text = $"{_btcPrice:F2}€";

            float btcPriceDiff = _btcPrice - CryptoSystem.Instance.lastBtcPrice;
            bool isBtcNegative = Single.IsNegative(btcPriceDiff);
            priceDiffText.text = $"{(isBtcNegative ? "" : "+")}{btcPriceDiff:F2}€";
            priceDiffText.color = isBtcNegative ? Color.red : Color.green;
        }
    }

    void UpdateEthereumPriceDisplay()
    {
        if (!_isBtcTab)
        {
            float _ethPrice = CryptoSystem.Instance.GetEthPrice();
            priceText.text = $"{_ethPrice:F2}€";

            float ethPriceDiff = _ethPrice - CryptoSystem.Instance.lastEthPrice;
            bool isEthNegative = Single.IsNegative(ethPriceDiff);
            priceDiffText.text = $"{(isEthNegative ? "" : "+")}{ethPriceDiff:F2}€";
            priceDiffText.color = isEthNegative ? Color.red : Color.green;
        }
    }

    private void UpdateExchangeUI()
    {
        AlertManager.Set(AlertType.Crypto, false);

        float _btcPrice = CryptoSystem.Instance.GetBtcPrice();
        float _ethPrice = CryptoSystem.Instance.GetEthPrice();
        float _btcPerHour = CryptoSystem.Instance.GetBtcPerHour();
        float _ethPerHour = CryptoSystem.Instance.GetEthPerHour();

        cryptoOutputText.text = "0";
        euroOutputText.text = "0";
        priceText.text = _isBtcTab ? $"{_btcPrice:F2}€" : $"{_ethPrice:F2}€";
        perHourText.text = _isBtcTab ? $"{_btcPerHour}/h" : $"{_ethPerHour}/h";
        minerAmountText.text = _isBtcTab ? $"{CryptoSystem.Instance.GetBtcMinerCount()}" : $"{CryptoSystem.Instance.GetEthMinerCount()}";
        string spriteName = _isBtcTab ? "bitcoin" : "ethereum"; 
        bigIcon.sprite = Resources.Load<Sprite>($"Images/GUI/Icons/Icons8/{spriteName}");
        buySectionText.text = $"Buy {(_isBtcTab ? "BTC" : "ETH")}";
        sellSectionText.text = $"Sell {(_isBtcTab ? "BTC" : "ETH")}";
        cryptoAmountPlaceholder.text = $"Enter {(_isBtcTab ? "BTC" : "ETH")} Amount";
        euroAmountPlaceholder.text = "Enter Euro Amount";
        marketEventText.text = CryptoSystem.Instance.currentMarketEvent;

        if (_isBtcTab)
        {
            float btcPriceDiff = _btcPrice - CryptoSystem.Instance.lastBtcPrice;
            bool isBtcNegative = Single.IsNegative(btcPriceDiff);
            priceDiffText.text = $"{(isBtcNegative ? "" : "+")}{btcPriceDiff:F2}€";
            priceDiffText.color = isBtcNegative ? Color.red : Color.green;
        }
        else
        {
            float ethPriceDiff = _ethPrice - CryptoSystem.Instance.lastEthPrice;
            bool isEthNegative = Single.IsNegative(ethPriceDiff);
            priceDiffText.text = $"{(isEthNegative ? "" : "+")}{ethPriceDiff:F2}€";
            priceDiffText.color = isEthNegative ? Color.red : Color.green;
        }
    }

    private void UpdateMiningUI()
    {
        AlertManager.Set(AlertType.Crypto, false);

        float _btcPrice = CryptoSystem.Instance.GetBtcPrice();
        float _ethPrice = CryptoSystem.Instance.GetEthPrice();
        float _btcPerHour = CryptoSystem.Instance.GetBtcPerHour();
        float _ethPerHour = CryptoSystem.Instance.GetEthPerHour();

        priceText.text = _isBtcTab ? $"{_btcPrice:F2}€" : $"{_ethPrice:F2}€";
        perHourText.text = _isBtcTab ? $"{_btcPerHour}/h" : $"{_ethPerHour}/h";
        minerAmountText.text = _isBtcTab ? $"{CryptoSystem.Instance.GetBtcMinerCount()}" : $"{CryptoSystem.Instance.GetEthMinerCount()}";
        string spriteName = _isBtcTab ? "bitcoin" : "ethereum"; 
        bigIcon.sprite = Resources.Load<Sprite>($"Images/GUI/Icons/Icons8/{spriteName}");
        minerPriceText.text = _isBtcTab ? "1 BTC" : "1 ETH";

        if (_isBtcTab)
        {
            float btcPriceDiff = _btcPrice - CryptoSystem.Instance.lastBtcPrice;
            bool isBtcNegative = Single.IsNegative(btcPriceDiff);
            priceDiffText.text = $"{(isBtcNegative ? "" : "+")}{btcPriceDiff:F2}€";
            priceDiffText.color = isBtcNegative ? Color.red : Color.green;
        }
        else
        {
            float ethPriceDiff = _ethPrice - CryptoSystem.Instance.lastEthPrice;
            bool isEthNegative = Single.IsNegative(ethPriceDiff);
            priceDiffText.text = $"{(isEthNegative ? "" : "+")}{ethPriceDiff:F2}€";
            priceDiffText.color = isEthNegative ? Color.red : Color.green;
        }
    }

    #region BUY / SELL

    public void BuyCrypto()
    {
        if (string.IsNullOrEmpty(euroAmountInput.text))
        {
            return;
        }

        if (float.TryParse(euroAmountInput.text, out float euroAmount))
        {
            float cryptoValue = (float)euroAmount / (_isBtcTab ? CryptoSystem.Instance.btcPrice : CryptoSystem.Instance.ethPrice);
            if (Globals.FloatUtil.GreaterOrEqual(PlayerManager.Instance.GetPlayerBalance(), cryptoValue))
            {
                PlayerManager.Instance.DeductCurrency(euroAmount);
                if (_isBtcTab)
                {
                    PlayerManager.Instance.AddBtc(cryptoValue);
                }
                else
                {
                    PlayerManager.Instance.AddEth(cryptoValue);
                };
            }
        }
    }

    public void SellCrypto()
    {
        if (string.IsNullOrEmpty(cryptoAmountInput.text))
        {
            return;
        }

        if (float.TryParse(cryptoAmountInput.text, out float cryptoAmount))
        {
            float cryptoPrice = _isBtcTab ? CryptoSystem.Instance.btcPrice : CryptoSystem.Instance.ethPrice;
            float cryptoOwned = _isBtcTab ? PlayerManager.Instance.GetBtcOwned() : PlayerManager.Instance.GetEthOwned();

            float euroValue = (float)cryptoAmount * cryptoPrice;
            if (Globals.FloatUtil.GreaterOrEqual(cryptoOwned, cryptoAmount))
            {
                PlayerManager.Instance.AddCurrency(euroValue);
                if (_isBtcTab)
                {
                    PlayerManager.Instance.DeductBtc(cryptoAmount);
                }
                else
                {
                    PlayerManager.Instance.DeductEth(cryptoAmount);
                };
            }
        }
    }

    public void RefreshEuroOutput()
    {
        if (string.IsNullOrEmpty(cryptoAmountInput.text))
        {
            return;
        }

        if (float.TryParse(cryptoAmountInput.text, out float cryptoAmount))
        {
            float euroValue = (float)cryptoAmount * (_isBtcTab ? CryptoSystem.Instance.btcPrice : CryptoSystem.Instance.ethPrice);
            euroOutputText.text = $"{euroValue:F2}€";
        }
        else
        {
            euroOutputText.text = "0";
        }
    }

    public void RefreshCryptoOutput()
    {
        if (string.IsNullOrEmpty(euroAmountInput.text))
        {
            return;
        }

        if (float.TryParse(euroAmountInput.text, out float euroAmount))
        {
            float cryptoPrice = _isBtcTab ? CryptoSystem.Instance.btcPrice : CryptoSystem.Instance.ethPrice;

            float cryptoValue = (float)euroAmount / cryptoPrice;
            cryptoOutputText.text = $"{cryptoValue:F8}";
        }
        else
        {
            cryptoOutputText.text = "0";
        }
    }

    public void BuyMaxCrypto()
    {
        float playerBal = PlayerManager.Instance.GetPlayerBalance();
        euroAmountInput.text = $"{playerBal}";
    }

    public void SellMaxCrypto()
    {
        float cryptoOwned = _isBtcTab ? PlayerManager.Instance.GetBtcOwned() : PlayerManager.Instance.GetEthOwned();
        cryptoAmountInput.text = $"{cryptoOwned}";
    }

    public bool BuyBTC(float euroAmount)
    {
        float _playerBal = PlayerManager.Instance.GetPlayerBalance();
        if (euroAmount > _playerBal)
            return false;

        PlayerManager.Instance.DeductCurrency(euroAmount);

        PlayerManager.Instance.AddBtc(euroAmount / CryptoSystem.Instance.btcPrice);

        UpdateExchangeUI();

        return true;
    }

    public bool BuyETH(float euroAmount)
    {
        float _playerBal = PlayerManager.Instance.GetPlayerBalance();
        if (euroAmount > _playerBal)
            return false;

        PlayerManager.Instance.DeductCurrency(euroAmount);

        PlayerManager.Instance.AddEth(euroAmount / CryptoSystem.Instance.ethPrice);

        UpdateExchangeUI();

        return true;
    }

    public bool SellBTC(float btcAmount)
    {

        if (btcAmount > PlayerManager.Instance.GetBtcOwned())
            return false;

        PlayerManager.Instance.DeductBtc(btcAmount);

        float euroAmount = btcAmount * CryptoSystem.Instance.btcPrice;
        PlayerManager.Instance.AddCurrency(euroAmount);

        UpdateExchangeUI();

        return true;
    }

    public bool SellETH(float ethAmount)
    {
        if (ethAmount > PlayerManager.Instance.GetEthOwned())
            return false;

        PlayerManager.Instance.DeductEth(ethAmount);

        float euroAmount = ethAmount * CryptoSystem.Instance.ethPrice;
        PlayerManager.Instance.AddCurrency(euroAmount);

        UpdateExchangeUI();

        return true;
    }

    public void ShowExchangeTab()
    {
        _isExchangeTab = true;
        exchangeTabObject.SetActive(true);
        miningTabObject.SetActive(false);
        UpdateExchangeUI();
    }

    public void ShowMiningTab()
    {
        _isExchangeTab = false;
        exchangeTabObject.SetActive(false);
        miningTabObject.SetActive(true);
        UpdateMiningUI();
    }

    public void ShowBtcTab()
    {
        _isBtcTab = true;
        if (_isExchangeTab)
        {
            UpdateExchangeUI();
        }
        else
        {
            UpdateMiningUI();
        }
    }

    public void ShowEthTab()
    {
        _isBtcTab = false;
        if (_isExchangeTab)
        {
            UpdateExchangeUI();
        }
        else
        {
            UpdateMiningUI();
        }
    }

    #endregion

    #region MINING

    public void AddMiningRig()
    {
        if (_isBtcTab)
        {
            CryptoSystem.Instance.AddBtcMiningRig();
            UpdateMiningUI();
        }
        else
        {
            CryptoSystem.Instance.AddEthMiningRig();
            UpdateMiningUI();
        }
    }

    public void RemoveMiningRig()
    {
        if (_isBtcTab)
        {
            CryptoSystem.Instance.RemoveBtcMiningRig();
            UpdateMiningUI();
        }
        else
        {
            CryptoSystem.Instance.RemoveEthMiningRig();
            UpdateMiningUI();
        }
    }

    public void UpdateBtcRigs()
    {
        for (int i = 0; i < CryptoSystem.Instance.GetBtcMinerCount(); i++)
        {
            // Instantiate mining rigs
        }
    }

    public void UpdateEthRigs()
    {
        for (int i = 0; i < CryptoSystem.Instance.GetEthMinerCount(); i++)
        {
            // Instantiate mining rigs
        }
    }

    #endregion
}