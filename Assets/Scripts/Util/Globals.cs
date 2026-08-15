using System;
using UnityEngine;
using System.Globalization;

public class Globals
{
    public const float EPSILON = 0.0001f;
    public const int EURO_DECIMAL = 2;
    public const int CRYPTO_DECIMAL = 8;
    public const float MarketBuyPriceModifier = 1.15f;
    public const float MarketSellPriceModifier = 0.85f;
    public const float MarketPriceMinFluctuation = 0.5f;
    public const float MarketPriceMaxFluctuation = 2f;
    public const string StatTrakUIText = "StatTrak™";

    public static Color GetHexColor(string hexColor)
    {
        Color color;

        if (ColorUtility.TryParseHtmlString(hexColor, out color))
        {
            return color;
        }
        else
        {
            return Color.red;
        }
    }

    public static string GetFormattedDateTime(DateTime dateTime)
    {
        return dateTime.ToLocalTime().ToString("dd'.'MM'.'yyyy @ HH':'mm':'ss");
    }

    public static string FormatCurrencyStr(float amount, int _n)
    {
        NumberFormatInfo nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        nfi.NumberGroupSeparator = ",";
        string currencyStrFormatted = amount.ToString($"N{_n}", nfi);
        return currencyStrFormatted;
    }

    public class FloatUtil
    {
        public static float Round(float f)
        {
            return Mathf.Round(f * 100f) / 100f;
        }

        public static float RoundCrypto(float f)
        {
            return Mathf.Round(f * 100000000f) / 100000000f;
        }

        public static bool Equal(float a, float b)
        {
            return Mathf.Abs(a - b) < EPSILON;
        }

        public static bool GreaterOrEqual(float a, float b)
        {
            return a > b - EPSILON;
        }

        public static bool LessOrEqual(float a, float b)
        {
            return a < b + EPSILON;
        }
    }
}