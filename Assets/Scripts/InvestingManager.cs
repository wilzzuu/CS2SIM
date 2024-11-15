using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InvestingManager : MonoBehaviour
{
    public static InvestingManager Instance { get; private set; }

    [Header("UI Elements")]
    public Transform stockCatalogGrid;       // Grid for displaying stock buttons
    public GameObject stockCatalogButtonPrefab;
    public GameObject stockDetailsPanel;    // Panel to show selected stock details
    public LineRenderer stockGraphRenderer; // LineRenderer for drawing graphs
    public TextMeshProUGUI stockNameText, stockPriceText, stockOwnedText, winChanceText;
    public TMP_InputField buyInputField, sellInputField;
    public Button buyButton, sellButton;

    [Header("Stock Settings")]
    public float updateInterval = 5f;       // Interval between price updates
    public int maxPriceHistoryPoints = 50;  // Limit on price history for graphing

    private List<Stock> _allStocks = new List<Stock>(); // All available stocks
    private List<PortfolioEntry> _portfolio = new List<PortfolioEntry>(); // Player-owned stocks
    private Stock _selectedStock;           // Currently selected stock

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Initialize stock data and UI
        LoadStocks();
        LoadPortfolio();
        PopulateStockCatalog();

        // Start updating stock prices
        StartCoroutine(UpdateStockPrices(updateInterval));

        // Setup button listeners
        buyButton.onClick.AddListener(() => BuyStock(_selectedStock.stockID, int.Parse(buyInputField.text)));
        sellButton.onClick.AddListener(() => SellStock(_selectedStock.stockID, int.Parse(sellInputField.text)));
    }

    private void LoadStocks()
    {
        // Mock data or load stock data from a file
        _allStocks = Resources.LoadAll<Stock>($"StockAssets").ToList();
    }

    private void PopulateStockCatalog()
    {
        foreach (Transform child in stockCatalogGrid)
        {
            Destroy(child.gameObject);
        }

        foreach (var stock in _allStocks)
        {
            GameObject buttonObj = Instantiate(stockCatalogButtonPrefab, stockCatalogGrid);
            buttonObj.GetComponentInChildren<TextMeshProUGUI>().text = stock.stockName;
            buttonObj.GetComponent<Button>().onClick.AddListener(() => ShowStockDetails(stock));
        }
    }

    private void ShowStockDetails(Stock stock)
    {
        _selectedStock = stock;
        stockDetailsPanel.SetActive(true);

        // Update stock details
        stockNameText.text = stock.stockName;
        stockPriceText.text = $"Price: {stock.currentPrice:F2}€";
        stockOwnedText.text = $"Owned: {GetStockQuantity(stock.stockID)}";

        // Draw graph
        DrawStockGraph(stock);
    }

    private void DrawStockGraph(Stock stock)
    {
        stockGraphRenderer.positionCount = stock.priceHistory.Count;

        for (int i = 0; i < stock.priceHistory.Count; i++)
        {
            stockGraphRenderer.SetPosition(i, new Vector3(i, stock.priceHistory[i], 0));
        }
    }

    private int GetStockQuantity(string stockID)
    {
        var entry = _portfolio.FirstOrDefault(e => e.stockID == stockID);
        return entry?.quantityOwned ?? 0;
    }

    public void BuyStock(string stockID, int quantity)
    {
        Stock stock = _allStocks.FirstOrDefault(s => s.stockID == stockID);
        if (stock == null) return;

        float totalCost = stock.currentPrice * quantity;
        if (PlayerManager.Instance.GetPlayerBalance() < totalCost)
        {
            Debug.Log("Not enough currency to buy stocks.");
            return;
        }

        PlayerManager.Instance.DeductCurrency(totalCost);

        var portfolioEntry = _portfolio.FirstOrDefault(e => e.stockID == stockID);
        if (portfolioEntry != null)
        {
            portfolioEntry.quantityOwned += quantity;
        }
        else
        {
            _portfolio.Add(ScriptableObject.CreateInstance<PortfolioEntry>());
        }

        stockOwnedText.text = $"Owned: {GetStockQuantity(stockID)}";
        SavePortfolio();
    }

    public void SellStock(string stockID, int quantity)
    {
        var portfolioEntry = _portfolio.FirstOrDefault(e => e.stockID == stockID);
        if (portfolioEntry == null || portfolioEntry.quantityOwned < quantity)
        {
            Debug.Log("Not enough stocks to sell.");
            return;
        }

        Stock stock = _allStocks.FirstOrDefault(s => s.stockID == stockID);
        if (stock == null) return;

        float totalValue = stock.currentPrice * quantity;

        portfolioEntry.quantityOwned -= quantity;
        PlayerManager.Instance.AddCurrency(totalValue);

        if (portfolioEntry.quantityOwned == 0)
        {
            _portfolio.Remove(portfolioEntry);
        }

        stockOwnedText.text = $"Owned: {GetStockQuantity(stockID)}";
        SavePortfolio();
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private IEnumerator UpdateStockPrices(float interval)
    {
        while (true)
        {
            foreach (var stock in _allStocks)
            {
                float randomVariation = Random.Range(-stock.priceChangeRate, stock.priceChangeRate);
                stock.currentPrice = Mathf.Max(0.1f, stock.currentPrice + randomVariation);
                stock.priceHistory.Add(stock.currentPrice);

                if (stock.priceHistory.Count > maxPriceHistoryPoints)
                {
                    stock.priceHistory.RemoveAt(0);
                }
            }

            TriggerRandomStockEvent();
            UpdateStockUI();
            yield return new WaitForSeconds(interval);
        }
    }

    private void TriggerRandomStockEvent()
    {
        if (Random.value < 0.1f)
        {
            Stock randomStock = _allStocks[Random.Range(0, _allStocks.Count)];
            bool isBoost = Random.value < 0.5f;

            float eventImpact = isBoost ? randomStock.currentPrice * 0.2f : -randomStock.currentPrice * 0.3f;
            randomStock.currentPrice = Mathf.Max(0.1f, randomStock.currentPrice + eventImpact);

            Debug.Log($"{randomStock.stockName} experienced a {(isBoost ? "boost" : "crash")}!");
        }
    }

    private void UpdateStockUI()
    {
        if (_selectedStock != null)
        {
            stockPriceText.text = $"Price: {_selectedStock.currentPrice:F2}€";
            DrawStockGraph(_selectedStock);
        }
    }

    private void SavePortfolio()
    {
        string json = JsonUtility.ToJson(ScriptableObject.CreateInstance<PortfolioData>());
        File.WriteAllText(Application.persistentDataPath + "/portfolio.json", json);
    }

    private void LoadPortfolio()
    {
        string path = Application.persistentDataPath + "/portfolio.json";
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            _portfolio = JsonUtility.FromJson<PortfolioData>(json).entries;
        }
    }
}
