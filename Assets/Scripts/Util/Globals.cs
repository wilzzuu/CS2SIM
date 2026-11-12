using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Globalization;

public class Globals
{
    public static bool AlwaysWin { get; set; }

    public const float EPSILON = 0.0001f;
    public const int EURO_DECIMAL = 2;
    public const int CRYPTO_DECIMAL = 8;
    public const int EURO_SCALE = 100;
    public const long CRYPTO_SCALE = 100000000L;
    public const float MarketBuyPriceModifier = 1.015f;
    public const float MarketSellPriceModifier = 0.985f;
    public const float MarketPriceMinFluctuation = 0.5f;
    public const float MarketPriceMaxFluctuation = 2f;
    public const string StatTrakUIText = "StatTrak™";
    public const string StatTrakUITextShort = "ST™";

    private static readonly Dictionary<ItemData, float> RandomSortKeys = new();

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

    public static string FormatUtcDateTime(DateTime dateTime)
    {
        return dateTime.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }

    public static DateTime ParseUtcDateTime(string value)
    {
        return DateTime.Parse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
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

    // Parses a user-entered decimal Euro string (e.g. "10.50" or "10,50") into whole cents.
    public static bool TryParseEuroInput(string text, out int cents)
    {
        // Replace comma with period to handle both decimal separators
        text = text.Replace(",", ".");

        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
        {
            cents = (int)Math.Round(value * EURO_SCALE, MidpointRounding.AwayFromZero);
            return true;
        }

        cents = 0;
        return false;
    }

    // Parses a user-entered decimal Crypto string (e.g. "0.00045678" or "0,00045678") into whole satoshi-like units.
    public static bool TryParseCryptoInput(string text, out long units)
    {
        // Replace comma with period to handle both decimal separators
        text = text.Replace(",", ".");

        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
        {
            units = (long)Math.Round(value * CRYPTO_SCALE, MidpointRounding.AwayFromZero);
            return true;
        }

        units = 0;
        return false;
    }

    public static int GetMarketBuyPrice(ItemData item)
    {
        return MoneyUtil.RoundEuro(item.price * MarketBuyPriceModifier);
    }

    public static List<ItemData> SortItems(IEnumerable<ItemData> items, string criteria, bool ascending)
    {
        List<ItemData> source = items.ToList();

        return criteria switch
        {
            "Type" => ascending
                ? source.OrderBy(item => item.type).ToList()
                : source.OrderByDescending(item => item.type).ToList(),
            "Rarity" => ascending
                ? source.OrderBy(item => RarityData.Order.TryGetValue(item.rarity, out int order) ? order : int.MaxValue).ToList()
                : source.OrderByDescending(item => RarityData.Order.TryGetValue(item.rarity, out int order) ? order : int.MinValue).ToList(),
            "Gun" => ascending
                ? source.OrderBy(item => item.gun).ToList()
                : source.OrderByDescending(item => item.gun).ToList(),
            "Condition" => ascending
                ? source.OrderBy(item => ConditionData.Order.TryGetValue(item.condition, out int order) ? order : int.MaxValue).ToList()
                : source.OrderByDescending(item => ConditionData.Order.TryGetValue(item.condition, out int order) ? order : int.MinValue).ToList(),
            "Price" => ascending
                ? source.OrderBy(item => item.price).ToList()
                : source.OrderByDescending(item => item.price).ToList(),
            "Random" => source.OrderBy(GetRandomSortKey).ToList(),
            _ => source
        };
    }

    public static void RandomizeItemSortOrder()
    {
        RandomSortKeys.Clear();
    }

    private static float GetRandomSortKey(ItemData item)
    {
        if (!RandomSortKeys.TryGetValue(item, out float key))
        {
            key = UnityEngine.Random.value;
            RandomSortKeys.Add(item, key);
        }

        return key;
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

        public static int CryptoToEuroCents(long cryptoUnits, int cryptoPriceCents)
        {
            decimal euroCents =
                (decimal)cryptoUnits * cryptoPriceCents / CRYPTO_SCALE;
            return checked((int)Math.Round(euroCents, MidpointRounding.AwayFromZero));
        }
    }
}