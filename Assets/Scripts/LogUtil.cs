using System;
using System.IO;
using UnityEngine;

public static class LogUtil
{
    private static string SaveFilePath
        {
            get
            {
                #if UNITY_EDITOR
                    return Path.Combine(Application.persistentDataPath, "EditorData");
                #else
                    return Path.Combine(Application.persistentDataPath, "SaveData");
                #endif
            }
        }

    private static readonly string LogDirectory =
        Path.Combine(SaveFilePath, "Logs");

    public static class Economy
    {
        public static void LogEuro(
            string action,
            float amount,
            float resultingBalance,
            string details = "")
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }

                string timestamp =
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                string sign =
                    amount >= 0 ? "+" : "";

                string entry =
                    $"[{timestamp}] " +
                    $"ACTION={action} | " +
                    $"AMOUNT={sign}{amount:F2}€ | " +
                    $"BALANCE={resultingBalance:F2}€";

                if (!string.IsNullOrWhiteSpace(details))
                {
                    entry += $" | DETAILS={details}";
                }

                File.AppendAllText(
                    GetLogPath("economy"),
                    entry + Environment.NewLine
                );
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"EconomyLogger failed: {e.Message}"
                );
            }
        }

        public static void LogCrypto(
            string action,
            float cryptoAmount,
            string cryptoType,
            float resultingBalance,
            string details = "")
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }

                string timestamp =
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                string sign =
                    cryptoAmount >= 0 ? "+" : "";

                string entry =
                    $"[{timestamp}] " +
                    $"ACTION={action} | " +
                    $"AMOUNT={sign}{cryptoAmount:F8} | " +
                    $"BALANCE={resultingBalance:F8} {cryptoType}";

                if (!string.IsNullOrWhiteSpace(details))
                {
                    entry += $" | DETAILS={details}";
                }

                File.AppendAllText(
                    GetLogPath("economy"),
                    entry + Environment.NewLine
                );
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"EconomyLogger failed: {e.Message}"
                );
            }
        }

        
    }

    public static class Event
    {
        
        public static void Log(
            string eventName,
            string action,
            string details = "")
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }

                string timestamp =
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                string entry =
                    $"[{timestamp}] " +
                    $"EVENT={eventName} | " +
                    $"ACTION={action} | ";

                if (!string.IsNullOrWhiteSpace(details))
                {
                    entry += $" | DETAILS={details}";
                }

                File.AppendAllText(
                    GetLogPath("event"),
                    entry + Environment.NewLine
                );
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"EconomyLogger failed: {e.Message}"
                );
            }
        }
    }

    public static class Console
    {
        public static void Log(
            string command,
            string details = "")
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }

                string timestamp =
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                string entry =
                    $"[{timestamp}] " +
                    $"COMMAND={command}";

                if (!string.IsNullOrWhiteSpace(details))
                {
                    entry += $" | DETAILS={details}";
                }

                File.AppendAllText(
                    GetLogPath("console"),
                    entry + Environment.NewLine
                );
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"ConsoleLogger failed: {e.Message}"
                );
            }
        }
    }

    public static string GetLogPath(string _loggerName)
    {
        string LogFile = Path.Combine(LogDirectory, $"{_loggerName}_{DateTime.Now:yyyy-MM-dd}.txt");
        return LogFile;
    }
}