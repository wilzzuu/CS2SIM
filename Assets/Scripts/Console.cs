using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Console : MonoBehaviour
{
    public static Console Instance;

    [SerializeField]
    public TMP_InputField inputField;

    private readonly Dictionary<string, Action<string[]>> _commands =
        new Dictionary<string, Action<string[]>>();

    private readonly List<string> _history =
        new List<string>();

    private int _historyIndex = -1;
    
    private const string HistoryKey = "ConsoleHistory";

    void Start()
    {
        LoadHistory();
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

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
                ConsoleOutput.Instance.AddLine($"Command '{command}' failed:\n{ex}");
            }
        }
        else
        {
            ConsoleOutput.Instance.AddLine($"Unknown command: {command}");
        }
    }

    public void Register(
        string command,
        Action<string[]> action)
    {
        _commands[command.ToLower()] = action;
    }

    
    private void Update()
    {
        if (!inputField.isFocused)
            return;

        if (Input.GetKeyDown(KeyCode.Return))
        {
            Submit();
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            PreviousCommand();
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            NextCommand();
        }
    }

    public void Submit()
    {
        string command = inputField.text.Trim();

        if (string.IsNullOrEmpty(command))
            return;

        ConsoleOutput.Instance.AddLine($"> {command}");

        Execute(command);

        AddToHistory(command);

        inputField.text = "";
        inputField.ActivateInputField();
    }

    private void AddToHistory(string command)
    {
        if (_history.Count == 0 ||
            _history[_history.Count - 1] != command)
        {
            _history.Add(command);
        }

        _historyIndex = _history.Count;
        SaveHistory();
    }

    private void PreviousCommand()
    {
        if (_history.Count == 0)
            return;

        _historyIndex--;

        if (_historyIndex < 0)
            _historyIndex = 0;

        inputField.text = _history[_historyIndex];

        inputField.caretPosition =
            inputField.text.Length;
    }

    private void NextCommand()
    {
        if (_history.Count == 0)
            return;

        _historyIndex++;

        if (_historyIndex >= _history.Count)
        {
            _historyIndex = _history.Count;
            inputField.text = "";
            return;
        }

        inputField.text = _history[_historyIndex];

        inputField.caretPosition =
            inputField.text.Length;
    }

    [System.Serializable]
    public class HistoryWrapper
    {
        public List<string> commands =
            new List<string>();
    }

    private void SaveHistory()
    {
        string json =
            JsonUtility.ToJson(
                new HistoryWrapper
                {
                    commands = _history
                });

        PlayerPrefs.SetString(
            HistoryKey,
            json);
    }

    private void LoadHistory()
    {
        if (!PlayerPrefs.HasKey(HistoryKey))
            return;

        string json =
            PlayerPrefs.GetString(HistoryKey);

        HistoryWrapper wrapper =
            JsonUtility.FromJson<HistoryWrapper>(json);

        _history.Clear();
        _history.AddRange(wrapper.commands);
    }

    #region Built-In Commands

    private void Help(string[] args)
    {
        foreach (var command in _commands.Keys)
        {
            ConsoleOutput.Instance.AddLine(command);
        }
    }

    private void Money(string[] args)
    {
        if (args.Length < 1)
        {
            ConsoleOutput.Instance.AddLine("money <amount>");
            return;
        }

        if (float.TryParse(args[0], out float amount))
        {
            PlayerManager.Instance.AddCurrency(amount);

            ConsoleOutput.Instance.AddLine($"Added {amount:F2}€");
        }
    }

    private void SetMoney(string[] args)
    {
        if (args.Length < 1)
        {
            ConsoleOutput.Instance.AddLine("setmoney <amount>");
            return;
        }

        if (float.TryParse(args[0], out float amount))
        {
            PlayerManager.Instance.player.balance = amount;
            PlayerManager.Instance.SavePlayerData();

            ConsoleOutput.Instance.AddLine($"Balance set to {amount:F2}€");
        }
    }

    private void GiveBTC(string[] args)
    {
        if (args.Length < 1)
        {
            ConsoleOutput.Instance.AddLine("givebtc <amount>");
            return;
        }

        if (float.TryParse(args[0], out float amount))
        {
            PlayerManager.Instance.AddBtc(amount);
            PlayerManager.Instance.SavePlayerData();

            ConsoleOutput.Instance.AddLine($"Added {amount} BTC");
        }
    }

    private void GiveETH(string[] args)
    {
        if (args.Length < 1)
        {
            ConsoleOutput.Instance.AddLine("giveeth <amount>");
            return;
        }

        if (float.TryParse(args[0], out float amount))
        {
            PlayerManager.Instance.AddEth(amount);
            PlayerManager.Instance.SavePlayerData();

            ConsoleOutput.Instance.AddLine($"Added {amount} ETH");
        }
    }

    private void LogMessage(string[] args)
    {
        string text =
            string.Join(" ", args);

        ConsoleOutput.Instance.AddLine(text);
    }

    #endregion
}

// COMMAND IDEAS
//caseopen 100
//inventoryvalue
//giveitem <id>
//setbtcprice 50000
//setethprice 3000
//marketevent
//minerbtc 10
//minereth 25
//autotrader 5
//dailyclaim
//giftclaim
//achievementunlock all