using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Linq;

public class Console : MonoBehaviour
{
    public static Console Instance;

    [SerializeField]
    public TMP_InputField inputField;

    private readonly Dictionary<string, ConsoleCommand> _commands =
        new Dictionary<string, ConsoleCommand>();

    private readonly List<string> _history =
        new List<string>();

    private Action<bool> _pendingConfirmation;
    private string _pendingPrompt;

    private static readonly string[] MarketEvents =
    {
        "bull",
        "crash",
        "rally",
        "correction",
        "optimism",
        "btc_manipulation",
        "eth_manipulation",
        "btc_insider_trading",
        "eth_insider_trading"
    };

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

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(
    string condition,
    string stackTrace,
    LogType type)
    {
        switch (type)
        {
            case LogType.Warning:
                ConsoleOutput.Instance.AddLine($"<color=yellow>{condition}</color>");
                break;

            case LogType.Error:
            case LogType.Exception:
                ConsoleOutput.Instance.AddLine($"<color=red>{condition}</color>");
                break;

            default:
                ConsoleOutput.Instance.AddLine(condition);
                break;
        }
    }

    private void RegisterCommands()
    {
        // Console
        Register("help", "Display help information", "help", 0, Help);

        // PlayerManager
        Register("addmoney", "Add money", "addmoney <amount>", 1, AddMoney);
        Register("deductmoney", "Deduct money", "deductmoney <amount>", 1, DeductMoney);
        Register("setmoney", "Set current money", "setmoney <amount>", 1, SetMoney);

        Register("setbtc", "Set Bitcoin amount", "setbtc <amount>", 1, SetBtc);
        Register("givebtc", "Give Bitcoin", "givebtc <amount>", 1, GiveBTC);
        Register("deductbtc", "Deduct Bitcoin", "deductbtc <amount>", 1, DeductBTC);
        Register("minebtc", "Mine Bitcoin", "minebtc <amount>", 1, MineBTC);

        Register("seteth", "Set Ethereum amount", "seteth <amount>", 1, SetEth);
        Register("giveeth", "Give Ethereum", "giveeth <amount>", 1, GiveETH);
        Register("deducteth", "Deduct Ethereum", "deducteth <amount>", 1, DeductETH);
        Register("mineeth", "Mine Ethereum", "mineeth <amount>", 1, MineETH);

        // CryptoSystem
        Register("setbtcprice", "Set Bitcoin price", "setbtcprice <price>", 1, SetBtcPrice);
        Register("addbtcminer", "Add Bitcoin miner", "addbtcminer <count>", 1, AddBtcMiner);
        Register("removebtcminer", "Remove Bitcoin miner", "removebtcminer <count>", 1, RemoveBtcMiner);
        Register("setethprice", "Set Ethereum price", "setethprice <price>", 1, SetEthPrice);
        Register("addethminer", "Add Ethereum miner", "addethminer <count>", 1, AddEthMiner);
        Register("removeethminer", "Remove Ethereum miner", "removeethminer <count>", 1, RemoveEthMiner);
        Register("randomcryptoevent", "Trigger a random crypto market event", "randomcryptoevent", 0, TriggerRandomCryptoEvent);
        Register("cryptoevent", "Trigger a crypto market event", "cryptoevent <event>", 1, TriggerCryptoEvent);

        // InventoryManager
        Register("giveitem", "Give an item by ID", "giveitem <item_id>", 1, GiveItem);
        Register("giverandomitem", "Give a random item", "giverandomitem", 0, GiveRandomItem);
        Register("playroulette", "Give 12 random items from roulette", "playroulette", 0, GiveRouletteItems);

        // SettingsManager
        Register("resetprogress", "Reset player progress", "resetprogress", 0, ResetProgress);

        // Log
        Register("log", "Log a message", "log <message>", 1, LogMessage);
    }

    public class ConsoleCommand
    {
        public string Name;
        public string Description;
        public string Usage;
        public int RequiredArgs;
        public Action<string[]> Action;
    }

    public void Execute(string commandLine)
    {
        if (_pendingConfirmation != null)
        {
            string response =
                commandLine.Trim().ToLower();

            if (response == "confirm")
            {
                Action<bool> callback =
                    _pendingConfirmation;

                _pendingConfirmation = null;
                _pendingPrompt = null;

                callback.Invoke(true);

                return;
            }

            if (response == "cancel")
            {
                Action<bool> callback =
                    _pendingConfirmation;

                _pendingConfirmation = null;
                _pendingPrompt = null;

                callback.Invoke(false);

                return;
            }

            LogUtil.Console.Log(
                "Please enter confirm or cancel."
            );

            return;
        }

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

        if (_commands.TryGetValue(command, out ConsoleCommand consoleCommand))
        {
            try
            {
                if (args.Length < consoleCommand.RequiredArgs)
                {
                    string message =
                        $"Usage: {consoleCommand.Usage}";

                    Debug.Log(message);
                    LogUtil.Console.Log(message);
                    return;
                }

                consoleCommand.Action.Invoke(args);
            }
            catch (Exception ex)
            {
                Debug.Log($"Command '{command}' failed:\n{ex}");
                LogUtil.Console.Log(
                    $"Command '{command}' failed:\n{ex}"
                );
            }
        }
        else
        {
            Debug.Log($"Unknown command: {command}");
            LogUtil.Console.Log(
                $"Unknown command: {command}"
            );
        }
    }

    public void RequestConfirmation(
    string prompt,
    Action<bool> callback)
    {
        _pendingPrompt = prompt;
        _pendingConfirmation = callback;

        LogUtil.Console.Log(
            $"{prompt} (confirm/cancel)"
        );
    }

    public void Register(
        string command,
        string description,
        string usage,
        int requiredArgs,
        Action<string[]> action)
    {
        _commands[command.ToLower()] = new ConsoleCommand
        {
            Name = command,
            Description = description,
            Usage = usage,
            RequiredArgs = requiredArgs,
            Action = action,
        };
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

        Debug.Log($"\n[{DateTime.UtcNow.ToString("dd'/'MM'/'yyyy @ HH':'mm':'ss")}] :: {command}");
        LogUtil.Console.Log(command);

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

    public static bool TryGetFloat(
    string[] args,
    int index,
    out float value)
    {
        value = 0;

        return args.Length > index &&
            float.TryParse(args[index], out value);
    }

    public static bool TryGetInt(
    string[] args,
    int index,
    out int value)
    {
        value = 0;

        return args.Length > index &&
            int.TryParse(args[index], out value);
    }

    #region Built-In Commands

    private void Help(string[] args)
    {
        if (args.Length > 0)
        {
            string command = args[0].ToLower();

            if (_commands.TryGetValue(command, out ConsoleCommand cmd))
            {
                Debug.Log($"Command: {cmd.Name}");
                Debug.Log($"Usage: {cmd.Usage}");
                Debug.Log($"Description: {cmd.Description}");
            }

            return;
        }

        foreach (var cmd in _commands.Values)
        {
            Debug.Log($"{cmd.Name} - {cmd.Description}");
        }
    }

    private void AddMoney(string[] args)
    {
        if (!TryGetFloat(args, 0, out float amount))
        {
            Debug.Log("Invalid amount. Please enter a valid number.");
            return;
        }

        PlayerManager.Instance.AddMoney(amount);
        Debug.Log($"Added {amount:F2}€");
    }

    private void DeductMoney(string[] args)
    {
        if (!TryGetFloat(args, 0, out float amount))
        {
            Debug.Log("Invalid amount. Please enter a valid number.");
            return;
        }

        PlayerManager.Instance.DeductMoney(amount);
        Debug.Log($"Deducted {amount:F2}€");
    }

    private void SetMoney(string[] args)
    {
        if (!TryGetFloat(args, 0, out float amount))
        {
            Debug.Log("Invalid amount. Please enter a valid number.");
            return;
        }

        PlayerManager.Instance.SetMoney(amount);

        Debug.Log($"Balance set to {amount:F2}€");
    }

    private void SetBtc(string[] args)
    {
        if (!TryGetFloat(args, 0, out float amount))
        {
            Debug.Log("Invalid amount. Please enter a valid number.");
            return;
        }

        PlayerManager.Instance.SetBtc(amount);

        Debug.Log($"Bitcoin amount set to {amount:F8} BTC");
    }

    private void GiveBTC(string[] args)
    {
        if (!TryGetFloat(args, 0, out float amount))
        {
            Debug.Log("Invalid amount. Please enter a valid number.");
            return;
        }

        PlayerManager.Instance.AddBtc(amount);

        Debug.Log($"Added {amount} BTC");
    }

    private void MineBTC(string[] args)
    {
        if (!TryGetFloat(args, 0, out float amount))
        {
            Debug.Log("Invalid amount. Please enter a valid number.");
            return;
        }

        PlayerManager.Instance.MineBtc(amount);

        Debug.Log($"Mined {amount} BTC");
    }

    private void DeductBTC(string[] args)
    {
        if (!TryGetFloat(args, 0, out float amount))
        {
            Debug.Log("Invalid amount. Please enter a valid number.");
            return;
        }

        PlayerManager.Instance.DeductBtc(amount);

        Debug.Log($"Deducted {amount} BTC");
    }

    private void SetEth(string[] args)
    {
        if (!TryGetFloat(args, 0, out float amount))
        {
            Debug.Log("Invalid amount. Please enter a valid number.");
            return;
        }

        PlayerManager.Instance.SetEth(amount);

        Debug.Log($"Ethereum amount set to {amount:F8} ETH");
    }

    private void GiveETH(string[] args)
    {
        if (!TryGetFloat(args, 0, out float amount))
        {
            Debug.Log("Invalid amount. Please enter a valid number.");
            return;
        }

        PlayerManager.Instance.AddEth(amount);

        Debug.Log($"Added {amount} ETH");
    }

    private void DeductETH(string[] args)
    {
        if (!TryGetFloat(args, 0, out float amount))
        {
            Debug.Log("Invalid amount. Please enter a valid number.");
            return;
        }

        PlayerManager.Instance.DeductEth(amount);

        Debug.Log($"Deducted {amount} ETH");
    }

    private void MineETH(string[] args)
    {
        if (!TryGetFloat(args, 0, out float amount))
        {
            Debug.Log("Invalid amount. Please enter a valid number.");
            return;
        }

        PlayerManager.Instance.MineEth(amount);

        Debug.Log($"Mined {amount} ETH");
    }

    private void SetBtcPrice(string[] args)
    {
        if (!TryGetFloat(args, 0, out float price))
        {
            Debug.Log("Invalid price. Please enter a valid number.");
            return;
        }

        CryptoSystem.Instance.SetBtcPrice(price);

        Debug.Log($"Bitcoin price set to {price:F2}€");
    }

    private void AddBtcMiner(string[] args)
    {
        if (!TryGetInt(args, 0, out int count))
        {
            Debug.Log("Invalid count. Please enter a valid number.");
            return;
        }

        CryptoSystem.Instance.SetBtcMinerCount(CryptoSystem.Instance.GetBtcMinerCount() + count);

        Debug.Log($"Added {count} BTC mining rig(s)");
    }

    private void RemoveBtcMiner(string[] args)
    {
        if (!TryGetInt(args, 0, out int count))
        {
            Debug.Log("Invalid count. Please enter a valid number.");
            return;
        }

        CryptoSystem.Instance.SetBtcMinerCount(CryptoSystem.Instance.GetBtcMinerCount() - count);

        Debug.Log($"Removed {count} BTC mining rig(s)");
    }

    private void SetEthPrice(string[] args)
    {
        if (!TryGetFloat(args, 0, out float price))
        {
            Debug.Log("Invalid price. Please enter a valid number.");
            return;
        }

        CryptoSystem.Instance.SetEthPrice(price);

        Debug.Log($"Ethereum price set to {price:F2}€");
    }

    private void AddEthMiner(string[] args)
    {
        if (!TryGetInt(args, 0, out int count))
        {
            Debug.Log("Invalid count. Please enter a valid number.");
            return;
        }

        CryptoSystem.Instance.SetEthMinerCount(CryptoSystem.Instance.GetEthMinerCount() + count);

        Debug.Log($"Added {count} ETH mining rig(s)");
    }

    private void RemoveEthMiner(string[] args)
    {
        if (!TryGetInt(args, 0, out int count))
        {
            Debug.Log("Invalid count. Please enter a valid number.");
            return;
        }

        CryptoSystem.Instance.SetEthMinerCount(CryptoSystem.Instance.GetEthMinerCount() - count);

        Debug.Log($"Removed {count} ETH mining rig(s)");
    }

    private void TriggerRandomCryptoEvent(string[] args)
    {
        CryptoSystem.Instance.GenerateMarketEvent();

        Debug.Log("Random crypto market event triggered.");
    }

    private void TriggerCryptoEvent(string[] args)
    {
        if (args.Length == 0)
        {
            Debug.Log("Please specify a crypto market event type.");
            return;
        }

        string eventType = args[0].ToLower();

        if (!MarketEvents.Contains(eventType))
        {
            Debug.Log(
                $"Invalid crypto market event.\n" +
                $"Available events:\n" +
                $"{string.Join(", ", MarketEvents)}"
            );

            return;
        }

        switch (eventType)
        {
            case "bull":
                CryptoSystem.Instance.btcPrice *= 1.15f;
                CryptoSystem.Instance.InvokeBtcPriceChanged();
                CryptoSystem.Instance.currentMarketEvent = "BTC Bull Run [+15%]";
                LogUtil.Event.Log(
                    "BTC Bull Run",
                    "BTC Price +15%"
                );
                break;

            case "crash":
                if (Globals.FloatUtil.GreaterOrEqual(CryptoSystem.Instance.btcPrice * 0.85f, CryptoSystem.Instance.btcMinMarketPrice))
                {
                    CryptoSystem.Instance.btcPrice *= 0.85f;
                    CryptoSystem.Instance.InvokeBtcPriceChanged();
                    CryptoSystem.Instance.currentMarketEvent = "BTC Crash [-15%]";
                    LogUtil.Event.Log(
                        "BTC Crash",
                        "BTC Price -15%"
                    );
                }
                break;

            case "rally":
                CryptoSystem.Instance.ethPrice *= 1.20f;
                CryptoSystem.Instance.InvokeEthPriceChanged();
                CryptoSystem.Instance.currentMarketEvent = "ETH Rally [+20%]";
                LogUtil.Event.Log(
                    "ETH Rally",
                    "ETH Price +20%");
                break;

            case "correction":
                if (Globals.FloatUtil.GreaterOrEqual(CryptoSystem.Instance.ethPrice * 0.80f, CryptoSystem.Instance.ethMinMarketPrice))
                {
                    CryptoSystem.Instance.ethPrice *= 0.80f;
                    CryptoSystem.Instance.InvokeEthPriceChanged();
                    CryptoSystem.Instance.currentMarketEvent = "ETH Correction [-20%]";
                    LogUtil.Event.Log(
                        "ETH Correction",
                        "ETH Price -20%"
                    );
                }
                break;

            case "optimism":
                CryptoSystem.Instance.btcPrice *= 1.05f;
                CryptoSystem.Instance.InvokeBtcPriceChanged();
                CryptoSystem.Instance.ethPrice *= 1.05f;
                CryptoSystem.Instance.InvokeEthPriceChanged();
                CryptoSystem.Instance.currentMarketEvent = "Market Optimism [+5%]";
                LogUtil.Event.Log(
                    "Market Optimism",
                    "BTC & ETH Price +5%"
                );
                break;

            case "btc_manipulation":
                if (Globals.FloatUtil.GreaterOrEqual(CryptoSystem.Instance.btcPrice * 0.5f, CryptoSystem.Instance.btcMinMarketPrice))
                {
                    CryptoSystem.Instance.btcPrice *= 0.5f;
                    CryptoSystem.Instance.InvokeBtcPriceChanged();
                    CryptoSystem.Instance.currentMarketEvent = "BTC Manipulation [-100%]";
                    LogUtil.Event.Log(
                        "BTC Manipulation",
                        "BTC Price -100%"
                    );
                }
                break;

            case "eth_manipulation":
                if (Globals.FloatUtil.GreaterOrEqual(CryptoSystem.Instance.ethPrice * 0.5f, CryptoSystem.Instance.ethMinMarketPrice))
                {
                    CryptoSystem.Instance.ethPrice *= 0.5f;
                    CryptoSystem.Instance.InvokeEthPriceChanged();
                    CryptoSystem.Instance.currentMarketEvent = "ETH Manipulation [-100%]";
                    LogUtil.Event.Log(
                        "ETH Manipulation",
                        "ETH Price -100%"
                    );
                }
                break;

            case "btc_insider_trading":
                CryptoSystem.Instance.btcPrice *= 2.0f;
                CryptoSystem.Instance.InvokeBtcPriceChanged();
                CryptoSystem.Instance.currentMarketEvent = "BTC Insider Trading [+100%]";
                LogUtil.Event.Log(
                    "BTC Insider Trading",
                    "BTC Price +100%"
                );
                break;

            case "eth_insider_trading":
                CryptoSystem.Instance.ethPrice *= 2.0f;
                CryptoSystem.Instance.InvokeEthPriceChanged();
                CryptoSystem.Instance.currentMarketEvent = "ETH Insider Trading [+100%]";
                LogUtil.Event.Log(
                    "ETH Insider Trading",
                    "ETH Price +100%"
                );
                break;
        }

        Debug.Log($"Market event '{eventType}' triggered.");
    }

    private void GiveItem(string[] args)
    {
        if (args.Length == 0)
        {
            Debug.Log("Please specify an item ID.");
            return;
        }

        string itemId = args[0];

        string itemPath = $"ItemAssets/{itemId}";
        ItemData item = Resources.Load<ItemData>(itemPath);

        if (item == null)
        {
            Debug.Log($"Item with ID '{itemId}' not found.");
            return;
        }

        InventoryManager.Instance.AddItemToInventory(item);
        Debug.Log($"Added item '{item.name}' to inventory.");
    }

    private void GiveRandomItem(string[] args)
    {
        InventoryManager.Instance.AddRandomItemToInventory();
        Debug.Log("Added a random item to inventory.");
    }

    private void GiveRouletteItems(string[] args)
    {
        for (int i = 0; i < 12; i++)
        {
            InventoryManager.Instance.AddRandomItemToInventory();
        }
        Debug.Log("Added 12 random items to inventory.");
    }

    private void ResetProgress(string[] args)
    {
        RequestConfirmation(
            "Are you sure you want to reset your progress? This action cannot be undone.",
            confirmed =>
            {
                if (confirmed)
                {
                    PlayerManager.Instance.ResetProgress();
                    SceneManager.LoadScene(1);
                    Debug.Log("Player progress has been reset.");
                }
                else
                {
                    Debug.Log("Reset progress canceled.");
                }
            });
    }

    private void LogMessage(string[] args)
    {
        string text =
            string.Join(" ", args);

        Debug.Log(text);
    }

    #endregion
}

// COMMAND IDEAS
//caseopen 100
//autotrader 5
//dailyclaim
//giftclaim
//achievementunlock all