using System;
using System.Collections.Generic;
using UnityEngine;

public class AutoTrader : MonoBehaviour
{
    [Header("References")]
    public PlayerData player;

    [Header("Settings")]
    public int traderTier;

    private readonly List<int> _btcHistory = new();
    private readonly List<int> _ethHistory = new();

    private const int HistorySize = 120;

    private readonly float[] BuyThresholds =
    {
        0f,
        0.03f,
        0.05f,
        0.08f,
        0.12f,
        0.18f
    };

    private readonly float[] SellThresholds =
    {
        0f,
        0.04f,
        0.06f,
        0.10f,
        0.15f,
        0.25f
    };

    private readonly float[] CapitalUsage =
    {
        0f,
        0.10f,
        0.15f,
        0.20f,
        0.25f,
        0.30f
    };

    public void RecordMarketPrices()
    {
        AddPrice(_btcHistory, CryptoSystem.Instance.GetBtcPrice());
        AddPrice(_ethHistory, CryptoSystem.Instance.GetEthPrice());
    }

    private void AddPrice(List<int> history, int price)
    {
        history.Add(price);

        if (history.Count > HistorySize)
            history.RemoveAt(0);
    }

    public void ExecuteAutoTrader()
    {
        if (traderTier <= 0)
            return;

        if (_btcHistory.Count < 20)
            return;

        TradeBTC();
        TradeETH();
    }

    private void TradeBTC()
    {
        float avg = Average(_btcHistory);
        int current = CryptoSystem.Instance.GetBtcPrice();

        float buyThreshold =
            avg * (1f - BuyThresholds[traderTier]);

        float sellThreshold =
            avg * (1f + SellThresholds[traderTier]);

        if (current <= buyThreshold)
        {
            BuyBTC(current);
        }
        else if (current >= sellThreshold)
        {
            SellBTC(current);
        }
    }

    private void TradeETH()
    {
        float avg = Average(_ethHistory);
        int current = CryptoSystem.Instance.GetEthPrice();

        float buyThreshold =
            avg * (1f - BuyThresholds[traderTier]);

        float sellThreshold =
            avg * (1f + SellThresholds[traderTier]);

        if (current <= buyThreshold)
        {
            BuyETH(current);
        }
        else if (current >= sellThreshold)
        {
            SellETH(current);
        }
    }

    private void BuyBTC(int price)
    {
        int budget = Globals.MoneyUtil.RoundEuro(
            player.balance *
            CapitalUsage[traderTier]);

        if (budget < 100 * Globals.EURO_SCALE)
            return;

        long btcAmount = (long)budget * Globals.CRYPTO_SCALE / price;

        player.balance -= budget;
        player.btcOwned += btcAmount;

        LogUtil.Event.Log(
            "AutoTrader BTC Buy",
            $"{(double)btcAmount / Globals.CRYPTO_SCALE:F6} BTC @ {Globals.FormatCurrencyStr(price, Globals.EURO_DECIMAL)}"
        );
    }

    private void SellBTC(int price)
    {
        if (player.btcOwned <= 0L)
            return;

        long amount = Globals.MoneyUtil.RoundCryptoUnits(player.btcOwned * 0.25);

        int revenue = (int)(amount * (long)price / Globals.CRYPTO_SCALE);

        player.btcOwned -= amount;
        player.balance += revenue;

        LogUtil.Event.Log(
            "AutoTrader BTC Sell",
            $"{(double)amount / Globals.CRYPTO_SCALE:F6} BTC @ {Globals.FormatCurrencyStr(price, Globals.EURO_DECIMAL)}"
        );
    }

    private void BuyETH(int price)
    {
        int budget = Globals.MoneyUtil.RoundEuro(
            player.balance *
            CapitalUsage[traderTier]);

        if (budget < 100 * Globals.EURO_SCALE)
            return;

        long ethAmount = (long)budget * Globals.CRYPTO_SCALE / price;

        player.balance -= budget;
        player.ethOwned += ethAmount;

        LogUtil.Event.Log(
            "AutoTrader ETH Buy",
            $"{(double)ethAmount / Globals.CRYPTO_SCALE:F6} ETH @ {Globals.FormatCurrencyStr(price, Globals.EURO_DECIMAL)}"
        );
    }

    private void SellETH(int price)
    {
        if (player.ethOwned <= 0L)
            return;

        long amount = Globals.MoneyUtil.RoundCryptoUnits(player.ethOwned * 0.25);

        int revenue = (int)(amount * (long)price / Globals.CRYPTO_SCALE);

        player.ethOwned -= amount;
        player.balance += revenue;

        LogUtil.Event.Log(
            "AutoTrader ETH Sell",
            $"{(double)amount / Globals.CRYPTO_SCALE:F6} ETH @ {Globals.FormatCurrencyStr(price, Globals.EURO_DECIMAL)}"
        );
    }

    private float Average(List<int> values)
    {
        float total = 0f;

        foreach (int value in values)
            total += value;

        return total / values.Count;
    }
}