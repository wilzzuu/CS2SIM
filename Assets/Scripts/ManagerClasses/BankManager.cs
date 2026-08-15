using System;
using System.IO;
using UnityEngine;

public class BankManager : MonoBehaviour
{
    public static BankManager Instance { get; private set; }

    private string SaveFilePath
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
    
    private const string SaveFileName = "BankData.save";

    
    public float bankBalance;

    public bool hasLoan;

    public float loanAmount;

    public float repaymentAmount;

    public DateTime dueDate;

    public int loanDays;

    private DateTime _nextInterestTick;

    private const float DailyBankInterest = 0.0005f; // 0.05%

    private const float LatePenalty = 0.10f; // 10%

    private void Awake()
    {
        Instance = this;

        LoadData();

        if (_nextInterestTick == default)
            _nextInterestTick = DateTime.UtcNow.AddDays(1);
    }

    private void Update()
    {
        CheckBankInterest();

        CheckLoanStatus();
    }

    #region Bank

    public bool Deposit(float amount)
    {
        if (PlayerManager.Instance.GetPlayerBalance() < amount)
            return false;

        PlayerManager.Instance.DeductMoney(amount);

        bankBalance += amount;

        SaveData();

        return true;
    }

    public bool Withdraw(float amount)
    {
        if (bankBalance < amount)
            return false;

        bankBalance -= amount;

        PlayerManager.Instance.AddMoney(amount);

        SaveData();

        return true;
    }

    private void CheckBankInterest()
    {
        if (DateTime.UtcNow < _nextInterestTick)
            return;

        float interest =
            bankBalance * DailyBankInterest;

        bankBalance += interest;

        _nextInterestTick =
            _nextInterestTick.AddDays(1);

        LogUtil.Economy.LogEuro(
            "Bank Interest",
            interest,
            bankBalance
        );

        SaveData();
    }

    #endregion

    #region Loans

    public bool TakeLoan(
        float amount,
        int days)
    {
        if (hasLoan)
            return false;

        float multiplier = GetInterestMultiplier(days);

        loanAmount = amount;

        repaymentAmount =
            amount * multiplier;

        dueDate =
            DateTime.UtcNow.AddDays(days);

        loanDays = days;

        hasLoan = true;

        PlayerManager.Instance.AddMoney(amount);

        LogUtil.Economy.LogEuro(
            "Loan Taken",
            amount,
            loanAmount,
            $"Repay {Globals.FormatCurrencyStr(repaymentAmount, Globals.EURO_DECIMAL)}€"
        );

        SaveData();

        return true;
    }

    public bool RepayLoan()
    {
        if (!hasLoan)
            return false;

        float availableFunds =
            PlayerManager.Instance.GetPlayerBalance() +
            bankBalance;

        if (availableFunds < repaymentAmount)
            return false;

        float remaining =
            repaymentAmount;

        float walletUsed =
            Mathf.Min(
                PlayerManager.Instance.GetPlayerBalance(),
                remaining);

        PlayerManager.Instance.DeductMoney(walletUsed);

        remaining -= walletUsed;

        if (remaining > 0)
        {
            bankBalance += remaining;
        }

        LogUtil.Economy.LogEuro(
            "Loan Repaid",
            repaymentAmount,
            loanAmount
        );

        ClearLoan();

        SaveData();

        return true;
    }

    private void CheckLoanStatus()
    {
        if (!hasLoan)
            return;

        if (DateTime.UtcNow < dueDate)
            return;

        repaymentAmount *=
            1f + LatePenalty;

        dueDate =
            dueDate.AddDays(1);

        LogUtil.Economy.LogEuro(
            "Loan Penalty",
            repaymentAmount,
            loanAmount,
            $"Debt increased to {Globals.FormatCurrencyStr(repaymentAmount, Globals.EURO_DECIMAL)}€"
        );

        SaveData();
    }

    private void ClearLoan()
    {
        hasLoan = false;

        loanAmount = 0;

        repaymentAmount = 0;

        loanDays = 0;
    }

    #endregion

    private float GetInterestMultiplier(int days)
    {
        switch (days)
        {
            case 1:
                return 1.05f;

            case 3:
                return 1.12f;

            case 7:
                return 1.25f;

            default:
                return 1.25f;
        }
    }

    #region Save

    public void SaveData()
    {
        BankSaveData data = new BankSaveData
        {
            bankBalance = bankBalance,
            hasLoan = hasLoan,
            loanAmount = loanAmount,
            repaymentAmount = repaymentAmount,
            dueDateTicks = _nextInterestTick.ToString("O"),
            loanDays = loanDays
        };

        string path = Path.Combine(SaveFilePath, SaveFileName);
        string jsonData = JsonUtility.ToJson(data);
        string encryptedData = DataEncryptionUtility.Encrypt(jsonData);

        File.WriteAllText(path, encryptedData);
    }

    public void LoadData()
    {
        string path = Path.Combine(SaveFilePath, SaveFileName);
        if (!File.Exists(path))
        {
            bankBalance = 0f;
            hasLoan = false;
            loanAmount = 0f;
            repaymentAmount = 0f;
            _nextInterestTick = DateTime.UtcNow;
            loanDays = 0;
        }

        string encryptedData = File.ReadAllText(path);
        string jsonData = DataEncryptionUtility.Decrypt(encryptedData);

        BankSaveData data = JsonUtility.FromJson<BankSaveData>(jsonData);

        bankBalance = data.bankBalance;
        hasLoan = data.hasLoan;
        loanAmount = data.loanAmount;
        repaymentAmount = data.repaymentAmount;
        _nextInterestTick = DateTime.Parse(
            data.dueDateTicks,
            null,
            System.Globalization.DateTimeStyles.AssumeUniversal
            | 
            System.Globalization.DateTimeStyles.AdjustToUniversal);
        loanDays = data.loanDays;
    }

    [System.Serializable]
    public class BankSaveData
    {
        public float bankBalance;

        public bool hasLoan;

        public float loanAmount;
        public float repaymentAmount;

        public string dueDateTicks;

        public int loanDays;
    }

    #endregion
}

