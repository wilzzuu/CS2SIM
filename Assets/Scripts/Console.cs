using System;
using System.Collections.Generic;
using UnityEngine;

public class Console : MonoBehaviour
{
    public static Console Instance;

    private readonly Dictionary<string, Action<string[]>> _commands =
        new Dictionary<string, Action<string[]>>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        RegisterCommands();
    }

    private void RegisterCommands()
    {
        Register("help", Help);

        Register("money", Money);
        Register("setmoney", SetMoney);

        Register("givebtc", GiveBTC);
        Register("giveeth", GiveETH);

        Register("log", LogMessage);
    }

    public void Execute(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
            return;

        string[] split =
            commandLine.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries);

        string command =
            split[0].ToLower();

        string[] args =
            new string[Mathf.Max(0, split.Length - 1)];

        for (int i = 1; i < split.Length; i++)
            args[i - 1] = split[i];

        if (_commands.TryGetValue(command, out Action<string[]> action))
        {
            try
            {
                action.Invoke(args);
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"Command '{command}' failed:\n{ex}");
            }
        }
        else
        {
            Debug.LogWarning(
                $"Unknown command: {command}");
        }
    }

    public void Register(
        string command,
        Action<string[]> action)
    {
        _commands[command.ToLower()] = action;
    }

    #region Built-In Commands

    private void Help(string[] args)
    {
        foreach (var command in _commands.Keys)
        {
            Debug.Log(command);
        }
    }

    private void Money(string[] args)
    {
        if (args.Length < 1)
        {
            Debug.Log("money <amount>");
            return;
        }

        if (float.TryParse(args[0], out float amount))
        {
            PlayerManager.Instance.AddCurrency(amount);

            Debug.Log($"Added {amount:F2}€");
        }
    }

    private void SetMoney(string[] args)
    {
        if (args.Length < 1)
        {
            Debug.Log("setmoney <amount>");
            return;
        }

        if (float.TryParse(args[0], out float amount))
        {
            PlayerManager.Instance.player.balance = amount;
            PlayerManager.Instance.SavePlayerData();

            Debug.Log($"Balance set to {amount:F2}€");
        }
    }

    private void GiveBTC(string[] args)
    {
        if (args.Length < 1)
        {
            Debug.Log("givebtc <amount>");
            return;
        }

        if (float.TryParse(args[0], out float amount))
        {
            PlayerManager.Instance.AddBtc(amount);
            PlayerManager.Instance.SavePlayerData();

            Debug.Log($"Added {amount} BTC");
        }
    }

    private void GiveETH(string[] args)
    {
        if (args.Length < 1)
        {
            Debug.Log("giveeth <amount>");
            return;
        }

        if (float.TryParse(args[0], out float amount))
        {
            PlayerManager.Instance.AddEth(amount);
            PlayerManager.Instance.SavePlayerData();

            Debug.Log($"Added {amount} ETH");
        }
    }

    private void LogMessage(string[] args)
    {
        string text =
            string.Join(" ", args);

        Debug.Log(text);
    }

    #endregion
}