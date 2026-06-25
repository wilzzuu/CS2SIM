using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance;

    [SerializeField]
    private List<AchievementData> achievements;

    private Dictionary<string, AchievementProgress> _progress =
        new Dictionary<string, AchievementProgress>();

    private void Awake()
    {
        Instance = this;
    }

    public bool IsCompleted(string achievementId)
    {
        return _progress.TryGetValue(
            achievementId,
            out AchievementProgress progress)
            && progress.completed;
    }

    public bool IsRewardClaimed(string achievementId)
    {
        return _progress.TryGetValue(
            achievementId,
            out AchievementProgress progress)
            && progress.rewardClaimed;
    }

    public void CheckAchievements()
    {
        foreach (AchievementData achievement in achievements)
        {
            if (IsCompleted(achievement.id))
                continue;

            if (EvaluateAchievement(achievement))
            {
                UnlockAchievement(achievement);
            }
        }
    }

    private bool EvaluateAchievement(
        AchievementData achievement)
    {
        return achievement.type switch
        {
            AchievementType.TotalBalance =>
                PlayerManager.Instance.player.balance >= achievement.targetValue,

            AchievementType.TotalCasesOpened =>
                StatisticsManager.Instance.totalCasesOpened >= achievement.targetValue,

            AchievementType.TotalItemsSold =>
                StatisticsManager.Instance.totalItemsSold >= achievement.targetValue,

            AchievementType.TotalCryptoMined =>
                StatisticsManager.Instance.totalCryptoMined >= achievement.targetValue,

            AchievementType.InventoryValue =>
                InventoryManager.Instance.CalculateInventoryValue() >= achievement.targetValue,

            _ => false
        };
    }

    private void UnlockAchievement(
        AchievementData achievement)
    {
        _progress[achievement.id] =
            new AchievementProgress
            {
                achievementId = achievement.id,
                completed = true
            };

        Debug.Log(
            $"Achievement Unlocked: {achievement.achievementName}");

        SaveData();
    }

    public void ClaimReward(
        AchievementData achievement)
    {
        if (!_progress.TryGetValue(
                achievement.id,
                out AchievementProgress progress))
            return;

        if (!progress.completed)
            return;

        if (progress.rewardClaimed)
            return;

        progress.rewardClaimed = true;

        PlayerManager.Instance.AddMoney(
            achievement.reward);

        SaveData();
    }

    private void SaveData()
    {
        // your save system
    }
}

[System.Serializable]
public class AchievementProgress
{
    public string achievementId;
    public bool completed;
    public bool rewardClaimed;
}