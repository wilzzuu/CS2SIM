using System;
using UnityEngine;
using System.Globalization;

public class Globals
{
    public const float EPSILON = 0.0001f;
    public const int EURO_DECIMAL = 2;
    public const int CRYPTO_DECIMAL = 8;
    public const int EURO_SCALE = 100;
    public const long CRYPTO_SCALE = 100000000L;
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

    public static string FormatCurrencyStr(long amount, int _n)
    {
        NumberFormatInfo nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        nfi.NumberGroupSeparator = ",";
        decimal scale = (decimal)Math.Pow(10, _n);
        decimal value = amount / scale;
        string currencyStrFormatted = value.ToString($"N{_n}", nfi);
        return currencyStrFormatted;
    }

    // Parses a user-entered decimal Euro string (e.g. "10.50") into whole cents.
    public static bool TryParseEuroInput(string text, out int cents)
    {
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
        {
            cents = (int)Math.Round(value * EURO_SCALE, MidpointRounding.AwayFromZero);
            return true;
        }

        cents = 0;
        return false;
    }

    // Parses a user-entered decimal Crypto string (e.g. "0.00045678") into whole satoshi-like units.
    public static bool TryParseCryptoInput(string text, out long units)
    {
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
        {
            units = (long)Math.Round(value * CRYPTO_SCALE, MidpointRounding.AwayFromZero);
            return true;
        }

        units = 0;
        return false;
    }

    // Converts float ratios/computations back into fixed-point currency amounts.
    public class MoneyUtil
    {
        public static int RoundEuro(float rawCents)
        {
            return Mathf.RoundToInt(rawCents);
        }

        public static long RoundCryptoUnits(double rawUnits)
        {
            return (long)Math.Round(rawUnits);
        }
    }
}