using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ConsoleUI : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField inputField;

    private readonly List<string> _history =
        new List<string>();

    private int _historyIndex = -1;
    
    private const string HistoryKey = "ConsoleHistory";

    void Start()
    {
        LoadHistory();
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
        string command =
            inputField.text.Trim();

        if (string.IsNullOrEmpty(command))
            return;

        Console.Instance.Execute(command);

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