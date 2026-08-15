using System;
using System.Collections.Generic;
using UnityEngine;

public class AutoTrader : MonoBehaviour
{
    [Header("References")]
    public PlayerData player;

    [Header("Settings")]
    public int traderTier;

    private readonly List<float> _btcHistory = new();
    private readonly List<float> _ethHistory = new();

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

    private void AddPrice(List<float> history, float price)
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
        float current = CryptoSystem.Instance.GetBtcPrice();

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
        float current = CryptoSystem.Instance.GetEthPrice();

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

    private void BuyBTC(float price)
    {
        float budget =
            player.balance *
            CapitalUsage[traderTier];

        if (budget < 100f)
            return;

        float btcAmount = budget / price;

        player.balance -= budget;
        player.btcOwned += btcAmount;

        LogUtil.Event.Log(
            "AutoTrader BTC Buy",
            $"{btcAmount:F6} BTC @ {Globals.FormatCurrencyStr(price, Globals.EURO_DECIMAL)}"
        );
    }

    private void SellBTC(float price)
    {
        if (player.btcOwned <= 0f)
            return;

        float amount =
            player.btcOwned * 0.25f;

        float revenue =
            amount * price;

        player.btcOwned -= amount;
        player.balance += revenue;

        LogUtil.Event.Log(
            "AutoTrader BTC Sell",
            $"{amount:F6} BTC @ {Globals.FormatCurrencyStr(price, Globals.EURO_DECIMAL)}"
        );
    }

    private void BuyETH(float price)
    {
        float budget =
            player.balance *
            CapitalUsage[traderTier];

        if (budget < 100f)
            return;

        float ethAmount = budget / price;

        player.balance -= budget;
        player.ethOwned += ethAmount;

        LogUtil.Event.Log(
            "AutoTrader ETH Buy",
            $"{ethAmount:F6} ETH @ {Globals.FormatCurrencyStr(price, Globals.EURO_DECIMAL)}"
        );
    }

    private void SellETH(float price)
    {
        if (player.ethOwned <= 0f)
            return;

        float amount =
            player.ethOwned * 0.25f;

        float revenue =
            amount * price;

        player.ethOwned -= amount;
        player.balance += revenue;

        LogUtil.Event.Log(
            "AutoTrader ETH Sell",
            $"{amount:F6} ETH @ {Globals.FormatCurrencyStr(price, Globals.EURO_DECIMAL)}"
        );
    }

    private float Average(List<float> values)
    {
        float total = 0f;

        foreach (float value in values)
            total += value;

        return total / values.Count;
    }
}