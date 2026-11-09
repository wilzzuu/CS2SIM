using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CryptoManager : MonoBehaviour
{
    public Button showExchangeTabButton;
    public Button showMiningTabButton;
    public Button showBtcTabButton;
    public Button showEthTabButton;
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
    public TextMeshProUGUI marketEventTimeText;

    void Start()
    {
        CryptoSystem.Instance.OnBtcPriceChanged += UpdateBitcoinPriceDisplay;
        CryptoSystem.Instance.OnEthPriceChanged += UpdateEthereumPriceDisplay;
        CryptoSystem.Instance.OnCurrentMarketEventChanged += UpdateMarketEventDisplay;
        showBtcTabButton.interactable = false;
        ShowExchangeTab();
    }

    void OnDestroy()
    {
        if (CryptoSystem.Instance != null)
        {
            CryptoSystem.Instance.OnBtcPriceChanged += UpdateBitcoinPriceDisplay;
            CryptoSystem.Instance.OnEthPriceChanged += UpdateEthereumPriceDisplay;
            CryptoSystem.Instance.OnCurrentMarketEventChanged += UpdateMarketEventDisplay;
        }
    }

    void UpdateBitcoinPriceDisplay()
    {
        if (_isBtcTab)
        {
            int _btcPrice = CryptoSystem.Instance.GetBtcPrice();
            priceText.text = $"{Globals.FormatCurrencyStr(_btcPrice, Globals.EURO_DECIMAL)}€";

            int btcPriceDiff = _btcPrice - CryptoSystem.Instance.lastBtcPrice;
            bool isBtcNegative = btcPriceDiff < 0;
            priceDiffText.text = $"{(isBtcNegative ? "" : "+")}{Globals.FormatCurrencyStr(btcPriceDiff, Globals.EURO_DECIMAL)}€";
            priceDiffText.color = isBtcNegative ? Color.red : Color.green;
        }
    }

    void UpdateEthereumPriceDisplay()
    {
        if (!_isBtcTab)
        {
            int _ethPrice = CryptoSystem.Instance.GetEthPrice();
            priceText.text = $"{Globals.FormatCurrencyStr(_ethPrice, Globals.EURO_DECIMAL)}€";

            int ethPriceDiff = _ethPrice - CryptoSystem.Instance.lastEthPrice;
            bool isEthNegative = ethPriceDiff < 0;
            priceDiffText.text = $"{(isEthNegative ? "" : "+")}{Globals.FormatCurrencyStr(ethPriceDiff, Globals.EURO_DECIMAL)}€";
            priceDiffText.color = isEthNegative ? Color.red : Color.green;
        }
    }

    private void UpdateExchangeUI()
    {
        AlertManager.Set(AlertType.Crypto, false);

        int _btcPrice = CryptoSystem.Instance.GetBtcPrice();
        int _ethPrice = CryptoSystem.Instance.GetEthPrice();
        long _btcPerHour = CryptoSystem.Instance.GetBtcPerHour();
        long _ethPerHour = CryptoSystem.Instance.GetEthPerHour();

        cryptoOutputText.text = "0";
        euroOutputText.text = "0";
        priceText.text = _isBtcTab ? $"{Globals.FormatCurrencyStr(_btcPrice, Globals.EURO_DECIMAL)}€" : $"{Globals.FormatCurrencyStr(_ethPrice, Globals.EURO_DECIMAL)}€";
        perHourText.text = _isBtcTab ? $"{Globals.FormatCurrencyStr(_btcPerHour, Globals.CRYPTO_DECIMAL)}/h" : $"{Globals.FormatCurrencyStr(_ethPerHour, Globals.CRYPTO_DECIMAL)}/h";
        minerAmountText.text = _isBtcTab ? $"{CryptoSystem.Instance.GetBtcMinerCount()}" : $"{CryptoSystem.Instance.GetEthMinerCount()}";
        string spriteName = _isBtcTab ? "BTC-860x860" : "ETH-860x860";
        bigIcon.sprite = Resources.Load<Sprite>($"Images/HighRes/{spriteName}");
        buySectionText.text = $"Buy {(_isBtcTab ? "BTC" : "ETH")}";
        sellSectionText.text = $"Sell {(_isBtcTab ? "BTC" : "ETH")}";
        cryptoAmountPlaceholder.text = $"Enter {(_isBtcTab ? "BTC" : "ETH")} Amount";
        euroAmountPlaceholder.text = "Enter Euro Amount";
        UpdateMarketEventDisplay();

        if (_isBtcTab)
        {
            int btcPriceDiff = _btcPrice - CryptoSystem.Instance.lastBtcPrice;
            bool isBtcNegative = btcPriceDiff < 0;
            priceDiffText.text = $"{(isBtcNegative ? "" : "+")}{Globals.FormatCurrencyStr(btcPriceDiff, Globals.EURO_DECIMAL)}€";
            priceDiffText.color = isBtcNegative ? Color.red : Color.green;
        }
        else
        {
            int ethPriceDiff = _ethPrice - CryptoSystem.Instance.lastEthPrice;
            bool isEthNegative = ethPriceDiff < 0;
            priceDiffText.text = $"{(isEthNegative ? "" : "+")}{Globals.FormatCurrencyStr(ethPriceDiff, Globals.EURO_DECIMAL)}€";
            priceDiffText.color = isEthNegative ? Color.red : Color.green;
        }
    }

    private void UpdateMiningUI()
    {
        AlertManager.Set(AlertType.Crypto, false);

        int _btcPrice = CryptoSystem.Instance.GetBtcPrice();
        int _ethPrice = CryptoSystem.Instance.GetEthPrice();
        long _btcPerHour = CryptoSystem.Instance.GetBtcPerHour();
        long _ethPerHour = CryptoSystem.Instance.GetEthPerHour();

        priceText.text = _isBtcTab ? $"{Globals.FormatCurrencyStr(_btcPrice, Globals.EURO_DECIMAL)}€" : $"{Globals.FormatCurrencyStr(_ethPrice, Globals.EURO_DECIMAL)}€";
        perHourText.text = _isBtcTab ? $"{Globals.FormatCurrencyStr(_btcPerHour, Globals.CRYPTO_DECIMAL)}/h" : $"{Globals.FormatCurrencyStr(_ethPerHour, Globals.CRYPTO_DECIMAL)}/h";
        minerAmountText.text = _isBtcTab ? $"{CryptoSystem.Instance.GetBtcMinerCount()}" : $"{CryptoSystem.Instance.GetEthMinerCount()}";
        string spriteName = _isBtcTab ? "BTC-860x860" : "ETH-860x860"; 
        bigIcon.sprite = Resources.Load<Sprite>($"Images/HighRes/{spriteName}");
        minerPriceText.text = _isBtcTab ? "1 BTC" : "1 ETH";

        if (_isBtcTab)
        {
            int btcPriceDiff = _btcPrice - CryptoSystem.Instance.lastBtcPrice;
            bool isBtcNegative = btcPriceDiff < 0;
            priceDiffText.text = $"{(isBtcNegative ? "" : "+")}{Globals.FormatCurrencyStr(btcPriceDiff, Globals.EURO_DECIMAL)}€";
            priceDiffText.color = isBtcNegative ? Color.red : Color.green;
        }
        else
        {
            int ethPriceDiff = _ethPrice - CryptoSystem.Instance.lastEthPrice;
            bool isEthNegative = ethPriceDiff < 0;
            priceDiffText.text = $"{(isEthNegative ? "" : "+")}{Globals.FormatCurrencyStr(ethPriceDiff, Globals.EURO_DECIMAL)}€";
            priceDiffText.color = isEthNegative ? Color.red : Color.green;
        }
    }

    private void UpdateMarketEventDisplay()
    {
        marketEventText.text = CryptoSystem.Instance.currentMarketEvent;
        marketEventTimeText.text = $"Next Event: {Globals.GetFormattedDateTime(CryptoSystem.Instance.nextMarketEvent.ToLocalTime())}";
    }

    #region BUY / SELL

    public void BuyCrypto()
    {
        if (string.IsNullOrEmpty(euroAmountInput.text))
        {
            return;
        }

        if (Globals.TryParseEuroInput(euroAmountInput.text, out int euroAmount))
        {
            long cryptoValue = (long)euroAmount * Globals.CRYPTO_SCALE / (_isBtcTab ? CryptoSystem.Instance.btcPrice : CryptoSystem.Instance.ethPrice);
            if (PlayerManager.Instance.GetPlayerBalance() >= euroAmount)
            {
                PlayerManager.Instance.DeductMoney(euroAmount);
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

        if (Globals.TryParseCryptoInput(cryptoAmountInput.text, out long cryptoAmount) && cryptoAmount > 0)
        {
            bool sold = _isBtcTab ? SellBTC(cryptoAmount) : SellETH(cryptoAmount);
            if (sold)
            {
                cryptoAmountInput.text = string.Empty;
            }
        }
    }

    public void RefreshEuroOutput()
    {
        if (string.IsNullOrEmpty(cryptoAmountInput.text))
        {
            return;
        }

        if (Globals.TryParseCryptoInput(cryptoAmountInput.text, out long cryptoAmount))
        {
            int euroValue = Globals.MoneyUtil.CryptoToEuroCents(
                cryptoAmount,
                _isBtcTab ? CryptoSystem.Instance.btcPrice : CryptoSystem.Instance.ethPrice);
            euroOutputText.text = $"{Globals.FormatCurrencyStr(euroValue, Globals.EURO_DECIMAL)}€";
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

        if (Globals.TryParseEuroInput(euroAmountInput.text, out int euroAmount))
        {
            int cryptoPrice = _isBtcTab ? CryptoSystem.Instance.btcPrice : CryptoSystem.Instance.ethPrice;

            long cryptoValue = (long)euroAmount * Globals.CRYPTO_SCALE / cryptoPrice;
            cryptoOutputText.text = $"{Globals.FormatCurrencyStr(cryptoValue, Globals.CRYPTO_DECIMAL)}";
        }
        else
        {
            cryptoOutputText.text = "0";
        }
    }

    public void BuyMaxCrypto()
    {
        int playerBal = PlayerManager.Instance.GetPlayerBalance();
        euroAmountInput.text = $"{playerBal / (float)Globals.EURO_SCALE}";
    }

    public void SellMaxCrypto()
    {
        long cryptoOwned = _isBtcTab ? PlayerManager.Instance.GetBtcOwned() : PlayerManager.Instance.GetEthOwned();
        cryptoAmountInput.text = Globals.FormatCurrencyStr(cryptoOwned, Globals.CRYPTO_DECIMAL);
        RefreshEuroOutput();
    }

    public bool BuyBTC(int euroAmount)
    {
        int _playerBal = PlayerManager.Instance.GetPlayerBalance();
        if (euroAmount > _playerBal)
            return false;

        PlayerManager.Instance.DeductMoney(euroAmount);

        PlayerManager.Instance.AddBtc((long)euroAmount * Globals.CRYPTO_SCALE / CryptoSystem.Instance.btcPrice);

        UpdateExchangeUI();

        return true;
    }

    public bool BuyETH(int euroAmount)
    {
        int _playerBal = PlayerManager.Instance.GetPlayerBalance();
        if (euroAmount > _playerBal)
            return false;

        PlayerManager.Instance.DeductMoney(euroAmount);

        PlayerManager.Instance.AddEth((long)euroAmount * Globals.CRYPTO_SCALE / CryptoSystem.Instance.ethPrice);

        UpdateExchangeUI();

        return true;
    }

    public bool SellBTC(long btcAmount)
    {
        if (btcAmount <= 0 || btcAmount > PlayerManager.Instance.GetBtcOwned())
            return false;

        PlayerManager.Instance.DeductBtc(btcAmount);

        int euroAmount = Globals.MoneyUtil.CryptoToEuroCents(btcAmount, CryptoSystem.Instance.btcPrice);
        PlayerManager.Instance.AddMoney(euroAmount);

        UpdateExchangeUI();

        return true;
    }

    public bool SellETH(long ethAmount)
    {
        if (ethAmount <= 0 || ethAmount > PlayerManager.Instance.GetEthOwned())
            return false;

        PlayerManager.Instance.DeductEth(ethAmount);

        int euroAmount = Globals.MoneyUtil.CryptoToEuroCents(ethAmount, CryptoSystem.Instance.ethPrice);
        PlayerManager.Instance.AddMoney(euroAmount);

        UpdateExchangeUI();

        return true;
    }

    public void ShowExchangeTab()
    {
        _isExchangeTab = true;
        exchangeTabObject.SetActive(true);
        miningTabObject.SetActive(false);
        showExchangeTabButton.interactable = false;
        showMiningTabButton.interactable = true;
        UpdateExchangeUI();
    }

    public void ShowMiningTab()
    {
        _isExchangeTab = false;
        exchangeTabObject.SetActive(false);
        miningTabObject.SetActive(true);
        showExchangeTabButton.interactable = true;
        showMiningTabButton.interactable = false;
        UpdateMiningUI();
    }

    public void ShowBtcTab()
    {
        _isBtcTab = true;
        showBtcTabButton.interactable = false;
        showEthTabButton.interactable = true;
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
        showBtcTabButton.interactable = true;
        showEthTabButton.interactable = false;
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