using System;
using UnityEngine;

public class Globals
{
    public const float EPSILON = 0.0001f;
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
        return dateTime.ToString("dd'.'MM'.'yyyy @ HH':'mm':'ss");
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