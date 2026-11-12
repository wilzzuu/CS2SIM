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

    private int _autoIndex = 0;
    private List<string> _autoMatches = new();

    private readonly List<string> _history =
        new List<string>();

    private Action<bool> _pendingConfirmation;
    private string _pendingPrompt;
    private bool _developerCommandsUnlocked;

    private int _historyIndex = -1;
    
    private const string HistoryKey = "ConsoleHistory";
    private const string DeveloperAccessCode = "anti";

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
        if (ConsoleOutput.Instance == null) return;

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

    #region Registration
    private void RegisterCommands()
    {
        // Console
        Register("help", "Display help information", "help", 0, Help, requiresUnlock: false);
        Register("unlock", "Unlock all developer commands using the access code", "unlock <code>", 1, UnlockCommand, requiresUnlock: false);
        Register("lock", "Lock all developer commands", "lock", 0, LockCommand, requiresUnlock: false);
        Register("alwayswin", "Toggle Always Win for wagered games", "alwayswin <on/off>", 1, SetAlwaysWin, new[] { "on", "off" });

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
        Register("cryptoevent", "Trigger a crypto market event", "cryptoevent <event>", 1, TriggerCryptoEvent, new[]
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
        });
        Register("resetcryptoprices", "Reset crypto prices to default", "resetcryptoprices", 0, ResetCryptoPrices);
        Register("resetbtcprice", "Reset Bitcoin price to default", "resetbtcprice", 0, ResetBtcPrice);
        Register("resetethprice", "Reset Ethereum price to default", "resetethprice", 0, ResetEthPrice);

        // InventoryManager
        Register("giveitem", "Give an item by ID", "giveitem <item_id>", 1, GiveItem);
        Register("giverandomitem", "Give a random item", "giverandomitem", 0, GiveRandomItem);
        Register("playroulette", "Give 12 random items from roulette", "playroulette", 0, GiveRouletteItems);

        // CollectionManager
        Register("collect", "Add an item to the collection by ID", "collect <item_id>", 1, CollectItem);
        Register("collectall", "Add all items to the collection", "collectall", 0, CollectAllItems);

        // SettingsManager
        Register("resetprogress", "Reset player progress", "resetprogress", 0, ResetProgress);

        // DailyRewardSystem
        Register("setcurrentday", "Set the current day", "setcurrentday <0-6>", 1, SetCurrentDay);
        Register("setclaimable", "Set whether the daily reward can be claimed", "setclaimable <true/false>", 1, SetClaimable);
        //Register("setdailyremaining", "Set daily reward remaining time", "setdailyremaining", 0, SetDailyRemaining);

        // Log
        Register("log", "Log a message", "log <message>", 1, LogMessage);
    }
    #endregion

    public class ConsoleCommand
    {
        public string Name;
        public string Description;
        public string Usage;
        public int RequiredArgs;
        public Action<string[]> Action;
        public string[] ValidArgs;
        public bool RequiresUnlock;
    }

    public List<string> GetAutocomplete(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new List<string>();

        input = input.ToLower();

        return _commands.Keys
            .Where(cmd =>
                cmd.StartsWith(input) ||
                cmd.Contains(input))
            .OrderBy(cmd => cmd.Length)
            .ToList();
    }

    public void AutoComplete()
    {
        string input = inputField.text;

        _autoMatches = GetAutocomplete(input);

        if (_autoMatches.Count == 0)
            return;

        if (_autoMatches.Count == 1)
        {
            inputField.text = _autoMatches[0] + " ";
            inputField.caretPosition = inputField.text.Length;

            _autoIndex = 0;
            return;
        }
    
        inputField.text = _autoMatches[_autoIndex];

        inputField.caretPosition = inputField.text.Length;

        ShowSuggestions(_autoMatches);
    }

    private void ShowSuggestions(List<string> matches)
    {
        ConsoleOutput.Instance.AddLine(
            string.Join(", ", matches)
        );
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

                LogUtil.Console.Log(
                    response,
                    null,
                    $"User confirmed: {_pendingPrompt}"
                );

                return;
            }

            if (response == "cancel")
            {
                Action<bool> callback =
                    _pendingConfirmation;

                _pendingConfirmation = null;
                _pendingPrompt = null;

                callback.Invoke(false);

                Debug.Log("Confirmation canceled.");
                LogUtil.Console.Log(
                    response,
                    null,
                    $"User canceled: {_pendingPrompt}"
                );

                return;
            }

            string message = "Invalid response. Please enter 'confirm' or 'cancel'.";
            Debug.Log(message);
            LogUtil.Console.Log(
                IsUnlockCommand(response) ? "unlock [redacted]" : response,
                message,
                "Invalid response to confirmation prompt."
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
                if (consoleCommand.RequiresUnlock && !_developerCommandsUnlocked)
                {
                    string message = "Developer commands are locked. Use 'unlock <code>' to enable them.";
                    Debug.Log(message);
                    LogUtil.Console.Log(command, message);
                    return;
                }

                if (args.Length < consoleCommand.RequiredArgs)
                {
                    string message =
                        $"Usage: {consoleCommand.Usage}";

                    Debug.Log(message);
                    LogUtil.Console.Log(
                        command,
                        message,
                        "Insufficient arguments provided."
                    );
                    return;
                }

                consoleCommand.Action.Invoke(args);
            }
            catch (Exception ex)
            {
                string message = $"Command '{command}' failed:\n{ex}";
                Debug.Log(message);
                LogUtil.Console.Log(
                    command,
                    message
                );
            }
        }
        else
        {
            string message = $"Unknown command: {command}";
            Debug.Log(message);
            LogUtil.Console.Log(
                command,
                message
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
            "SYSTEM PROMPT",
            $"{prompt} (confirm/cancel)"
        );
    }

    public void Register(
        string command,
        string description,
        string usage,
        int requiredArgs,
        Action<string[]> action,
        string[] ValidArgs = null,
        bool requiresUnlock = true)
    {
        _commands[command.ToLower()] = new ConsoleCommand
        {
            Name = command,
            Description = description,
            Usage = usage,
            RequiredArgs = requiredArgs,
            Action = action,
            ValidArgs = ValidArgs,
            RequiresUnlock = requiresUnlock
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

        bool isUnlock = IsUnlockCommand(command);
        string loggedCommand = isUnlock ? "unlock [redacted]" : command;
        Debug.Log($"\n[{Globals.GetFormattedDateTime(DateTime.UtcNow)}] :: {loggedCommand}");
        LogUtil.Console.Log(
            loggedCommand,
            null,
            "User submitted command"
        );

        Execute(command);

        if (!isUnlock)
            AddToHistory(command);

        inputField.text = "";
        inputField.ActivateInputField();
    }

    private static bool IsUnlockCommand(string commandLine)
    {
        string trimmed = commandLine.TrimStart();
        return trimmed.Equals("unlock", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("unlock ", StringComparison.OrdinalIgnoreCase);
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

    public static bool TryGetEuro(
    string[] args,
    int index,
    out int value)
    {
        value = 0;

        return args.Length > index &&
            Globals.TryParseEuroInput(args[index], out value);
    }

    public static bool TryGetCrypto(
    string[] args,
    int index,
    out long value)
    {
        value = 0;

        return args.Length > index &&
            Globals.TryParseCryptoInput(args[index], out value);
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
                if (cmd.RequiresUnlock)
                {
                    Debug.Log($"Access: {(_developerCommandsUnlocked ? "Unlocked" : "Locked")}");
                }
                if (cmd.ValidArgs != null)
                {
                    Debug.Log($"Valid Arguments: {string.Join(", ", cmd.ValidArgs)}");
                }
            }
            else
            {
                Debug.Log($"Unknown command: {command}");
            }

            return;
        }

        foreach (var cmd in _commands.Values)
        {
            string access = cmd.RequiresUnlock
                ? (_developerCommandsUnlocked ? " [UNLOCKED]" : " [LOCKED]")
                : "";
            Debug.Log($"{cmd.Name}{access} - {cmd.Description}");
        }
    }

    private void UnlockCommand(string[] args)
    {
        if (args.Length != 1)
        {
            Debug.Log("Usage: unlock <code>");
            return;
        }

        if (!string.Equals(args[0], DeveloperAccessCode, StringComparison.Ordinal))
        {
            Debug.Log("Invalid developer access code.");
            return;
        }

        SetDeveloperCommandsLock(false);
    }

    private void LockCommand(string[] args)
    {
        SetDeveloperCommandsLock(true);
    }

    private void SetDeveloperCommandsLock(bool locked)
    {
        _developerCommandsUnlocked = !locked;
        Debug.Log(locked ? "All developer commands locked." : "All developer commands unlocked.");
    }

    private void SetAlwaysWin(string[] args)
    {
        string value = args[0].ToLower();
        if (value != "on" && value != "off")
        {
            Debug.Log("Usage: alwayswin <on/off>");
            return;
        }

        Globals.AlwaysWin = value == "on";
        Debug.Log($"Always Win {(Globals.AlwaysWin ? "enabled" : "disabled")}.");
    }

    private void AddMoney(string[] args)
    {
        if (!TryGetEuro(args, 0, out int amount))
        {
            Debug.Log("Invalid amount. Please enter a valid number.");
            return;
        }

        PlayerManager.Instance.AddMoney(amount);
        Debug.Log($"Added {Globals.FormatCurrencyStr(amount, Globals.EURO_DECIMAL)}€");
    }

    private void DeductMoney(string[] args)
    {
        if (!TryGetEuro(args, 0, out int amount))
        {
            Debug.Log("Invalid amount. Please enter a valid number.");
            return;
        }

        PlayerManager.Instance.DeductMoney(amount);
        Debug.Log($"Deducted {Globals.FormatCurrencyStr(amount, Globals.EURO_DECIMAL)}€");
    }

    private void SetMoney(string[] args)
    {
        if (!TryGetEuro(args, 0, out int amount))
        {
            Debug.Log("Invalid amount. Please enter a valid number.");
            return;
        }

        PlayerManager.Instance.SetMoney(amount);

        Debug.Log($"Balance set to {Globals.FormatCurrencyStr(amount, Globals.EURO_DECIMAL)}€");
    }

    private void SetBtc(string[] args)
    {
        if (!TryGetCrypto(args, 0, out long amount))
        {
            Debug.Log("Invalid amount. Please enter a valid number.");
            return;
        }

        PlayerManager.Instance.SetBtc(amount);

        Debug.Log($"Bitcoin amount set to {Globals.FormatCurrencyStr(amount, Globals.CRYPTO_DECIMAL)} BTC");
    }

    private void GiveBTC(string[] args)
    {
        if (!TryGetCrypto(args, 0, out long amount))
        {
            Debug.Log("Invalid amount. Please enter a valid number.");
            return;
        }

        PlayerManager.Instance.AddBtc(amount);

        Debug.Log($"Added {Globals.FormatCurrencyStr(amount, Globals.CRYPTO_DECIMAL)} BTC");
    }

    private void MineBTC(string[] args)
    {
        if (!TryGetCrypto(args, 0, out long amount))
        {
            Debug.Log("Invalid amount. Please enter a valid number.");
            return;
        }

        PlayerManager.Instance.MineBtc(amount);

        Debug.Log($"Mined {Globals.FormatCurrencyStr(amount, Globals.CRYPTO_DECIMAL)} BTC");
    }

    private void DeductBTC(string[] args)
    {
        if (!TryGetCrypto(args, 0, out long amount))
        {
            Debug.Log("Invalid amount. Please enter a valid number.");
            return;
        }

        PlayerManager.Instance.DeductBtc(amount);

        Debug.Log($"Deducted {Globals.FormatCurrencyStr(amount, Globals.CRYPTO_DECIMAL)} BTC");
    }

    private void SetEth(string[] args)
    {
        if (!TryGetCrypto(args, 0, out long amount))
        {
            Debug.Log("Invalid amount. Please enter a valid number.");
            return;
        }

        PlayerManager.Instance.SetEth(amount);

        Debug.Log($"Ethereum amount set to {Globals.FormatCurrencyStr(amount, Globals.CRYPTO_DECIMAL)} ETH");
    }

    private void GiveETH(string[] args)
    {
        if (!TryGetCrypto(args, 0, out long amount))
        {
            Debug.Log("Invalid amount. Please enter a valid number.");
            return;
        }

        PlayerManager.Instance.AddEth(amount);

        Debug.Log($"Added {Globals.FormatCurrencyStr(amount, Globals.CRYPTO_DECIMAL)} ETH");
    }

    private void DeductETH(string[] args)
    {
        if (!TryGetCrypto(args, 0, out long amount))
        {
            Debug.Log("Invalid amount. Please enter a valid number.");
            return;
        }

        PlayerManager.Instance.DeductEth(amount);

        Debug.Log($"Deducted {Globals.FormatCurrencyStr(amount, Globals.CRYPTO_DECIMAL)} ETH");
    }

    private void MineETH(string[] args)
    {
        if (!TryGetCrypto(args, 0, out long amount))
        {
            Debug.Log("Invalid amount. Please enter a valid number.");
            return;
        }

        PlayerManager.Instance.MineEth(amount);

        Debug.Log($"Mined {Globals.FormatCurrencyStr(amount, Globals.CRYPTO_DECIMAL)} ETH");
    }

    private void SetBtcPrice(string[] args)
    {
        if (!TryGetEuro(args, 0, out int price))
        {
            Debug.Log("Invalid price. Please enter a valid number.");
            return;
        }

        CryptoSystem.Instance.SetBtcPrice(price);

        Debug.Log($"Bitcoin price set to {Globals.FormatCurrencyStr(price, Globals.EURO_DECIMAL)}€");
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
        if (!TryGetEuro(args, 0, out int price))
        {
            Debug.Log("Invalid price. Please enter a valid number.");
            return;
        }

        CryptoSystem.Instance.SetEthPrice(price);

        Debug.Log($"Ethereum price set to {Globals.FormatCurrencyStr(price, Globals.EURO_DECIMAL)}€");
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

        Debug.Log($"Random crypto market event triggered: {CryptoSystem.Instance.currentMarketEvent}");
    }

    private void TriggerCryptoEvent(string[] args)
    {
        if (args.Length == 0)
        {
            Debug.Log("Please specify a crypto market event type.");
            return;
        }

        string eventType = args[0].ToLower();

        if (!_commands.TryGetValue("cryptoevent", out ConsoleCommand cmd) ||
            cmd.ValidArgs == null ||
            !cmd.ValidArgs.Contains(eventType))
        {
            Debug.Log(
                $"Invalid crypto market event.\n" +
                $"Available events:\n" +
                $"{string.Join(", ", cmd.ValidArgs ?? new string[0])}"
            );

            return;
        }

        switch (eventType)
        {
            case "bull":
                CryptoSystem.Instance.btcPrice = Globals.MoneyUtil.RoundEuro(CryptoSystem.Instance.btcPrice * 1.15f);
                CryptoSystem.Instance.InvokeBtcPriceChanged();
                CryptoSystem.Instance.currentMarketEvent = "BTC Bull Run [+15%]";
                LogUtil.Event.Log(
                    "BTC Bull Run",
                    "BTC Price +15%"
                );
                break;

            case "crash":
                if (CryptoSystem.Instance.btcPrice * 0.85f >= CryptoSystem.Instance.btcMinMarketPrice)
                {
                    CryptoSystem.Instance.btcPrice = Globals.MoneyUtil.RoundEuro(CryptoSystem.Instance.btcPrice * 0.85f);
                    CryptoSystem.Instance.InvokeBtcPriceChanged();
                    CryptoSystem.Instance.currentMarketEvent = "BTC Crash [-15%]";
                    LogUtil.Event.Log(
                        "BTC Crash",
                        "BTC Price -15%"
                    );
                }
                break;

            case "rally":
                CryptoSystem.Instance.ethPrice = Globals.MoneyUtil.RoundEuro(CryptoSystem.Instance.ethPrice * 1.20f);
                CryptoSystem.Instance.InvokeEthPriceChanged();
                CryptoSystem.Instance.currentMarketEvent = "ETH Rally [+20%]";
                LogUtil.Event.Log(
                    "ETH Rally",
                    "ETH Price +20%");
                break;

            case "correction":
                if (CryptoSystem.Instance.ethPrice * 0.80f >= CryptoSystem.Instance.ethMinMarketPrice)
                {
                    CryptoSystem.Instance.ethPrice = Globals.MoneyUtil.RoundEuro(CryptoSystem.Instance.ethPrice * 0.80f);
                    CryptoSystem.Instance.InvokeEthPriceChanged();
                    CryptoSystem.Instance.currentMarketEvent = "ETH Correction [-20%]";
                    LogUtil.Event.Log(
                        "ETH Correction",
                        "ETH Price -20%"
                    );
                }
                break;

            case "optimism":
                CryptoSystem.Instance.btcPrice = Globals.MoneyUtil.RoundEuro(CryptoSystem.Instance.btcPrice * 1.05f);
                CryptoSystem.Instance.InvokeBtcPriceChanged();
                CryptoSystem.Instance.ethPrice = Globals.MoneyUtil.RoundEuro(CryptoSystem.Instance.ethPrice * 1.05f);
                CryptoSystem.Instance.InvokeEthPriceChanged();
                CryptoSystem.Instance.currentMarketEvent = "Market Optimism [+5%]";
                LogUtil.Event.Log(
                    "Market Optimism",
                    "BTC & ETH Price +5%"
                );
                break;

            case "btc_manipulation":
                if (CryptoSystem.Instance.btcPrice * 0.5f >= CryptoSystem.Instance.btcMinMarketPrice)
                {
                    CryptoSystem.Instance.btcPrice = Globals.MoneyUtil.RoundEuro(CryptoSystem.Instance.btcPrice * 0.5f);
                    CryptoSystem.Instance.InvokeBtcPriceChanged();
                    CryptoSystem.Instance.currentMarketEvent = "BTC Manipulation [-100%]";
                    LogUtil.Event.Log(
                        "BTC Manipulation",
                        "BTC Price -100%"
                    );
                }
                break;

            case "eth_manipulation":
                if (CryptoSystem.Instance.ethPrice * 0.5f >= CryptoSystem.Instance.ethMinMarketPrice)
                {
                    CryptoSystem.Instance.ethPrice = Globals.MoneyUtil.RoundEuro(CryptoSystem.Instance.ethPrice * 0.5f);
                    CryptoSystem.Instance.InvokeEthPriceChanged();
                    CryptoSystem.Instance.currentMarketEvent = "ETH Manipulation [-100%]";
                    LogUtil.Event.Log(
                        "ETH Manipulation",
                        "ETH Price -100%"
                    );
                }
                break;

            case "btc_insider_trading":
                CryptoSystem.Instance.btcPrice = Globals.MoneyUtil.RoundEuro(CryptoSystem.Instance.btcPrice * 2.0f);
                CryptoSystem.Instance.InvokeBtcPriceChanged();
                CryptoSystem.Instance.currentMarketEvent = "BTC Insider Trading [+100%]";
                LogUtil.Event.Log(
                    "BTC Insider Trading",
                    "BTC Price +100%"
                );
                break;

            case "eth_insider_trading":
                CryptoSystem.Instance.ethPrice = Globals.MoneyUtil.RoundEuro(CryptoSystem.Instance.ethPrice * 2.0f);
                CryptoSystem.Instance.InvokeEthPriceChanged();
                CryptoSystem.Instance.currentMarketEvent = "ETH Insider Trading [+100%]";
                LogUtil.Event.Log(
                    "ETH Insider Trading",
                    "ETH Price +100%"
                );
                break;
        }

        CryptoSystem.Instance.InvokeCryptoEventChanged();
        Debug.Log($"Market event '{eventType}' triggered.");
    }

    private void ResetCryptoPrices(string[] args)
    {
        CryptoSystem.Instance.ResetCryptoPrices();
        Debug.Log("Crypto prices have been reset to default.");
    }

    private void ResetBtcPrice(string[] args)
    {
        CryptoSystem.Instance.ResetBtcPrice();
        Debug.Log("Bitcoin price has been reset to default.");
    }

    private void ResetEthPrice(string[] args)
    {
        CryptoSystem.Instance.ResetEthPrice();
        Debug.Log("Ethereum price has been reset to default.");
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

    private void CollectItem(string[] args)
    {
        string itemId = args[0];
        ItemData item = Resources.Load<ItemData>($"ItemAssets/{itemId}");
        if (item == null)
        {
            Debug.Log($"Item with ID '{itemId}' not found.");
            return;
        }
        if (string.IsNullOrWhiteSpace(item.id))
        {
            Debug.LogError($"Item with ID '{itemId}' has no collection ID.");
            return;
        }

        if (CollectionManager.IsCollected(item.id))
        {
            Debug.Log($"Item '{itemId}' is already in the collection.");
            return;
        }

        CollectionManager.RecordItem(item);
        Debug.Log($"Added item '{itemId}' to the collection.");
    }

    private void CollectAllItems(string[] args)
    {
        ItemData[] items = Resources.LoadAll<ItemData>("ItemAssets");
        if (items.Length == 0)
        {
            Debug.LogWarning("No collection items were found.");
            return;
        }

        List<ItemData> validItems = new List<ItemData>();
        foreach (ItemData item in items)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.id))
            {
                Debug.LogWarning("Skipping a collection item with a missing asset or ID.");
                continue;
            }
            validItems.Add(item);
        }

        int addedCount = CollectionManager.RecordItems(validItems);
        Debug.Log($"Added {addedCount} item(s) to the collection.");
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

    private void SetCurrentDay(string[] args)
    {
        if (!TryGetInt(args, 0, out int day))
        {
            Debug.Log("Invalid day. Please enter a valid number.");
            return;
        }

        if (day < 0 || day > 6)
        {
            Debug.Log("Day must be between 0 and 6.");
            return;
        }

        DailyRewardSystem.Instance.currentDay = day;
        Debug.Log($"Current day set to {day}.");
    }

    private void SetClaimable(string[] args)
    {
        if (args.Length == 0)
        {
            Debug.Log("Please specify 'true' or 'false'.");
            return;
        }

        string input = args[0].ToLower();

        if (input == "true")
        {
            DailyRewardSystem.Instance.lastClaim = DateTime.UtcNow.AddDays(-1);
        }
        else if (input == "false")
        {
            DailyRewardSystem.Instance.lastClaim = DateTime.UtcNow;
        }
        else
        {
            Debug.Log("Invalid input. Please specify 'true' or 'false'.");
            return;
        }

        Debug.Log($"Daily reward claimable set to '{input}'.");
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