using UnityEngine;

[CreateAssetMenu(menuName = "Achievements/Achievement")]
public class AchievementData : ScriptableObject
{
    public string id;

    [Header("Display")]
    public string achievementName;
    [TextArea]
    public string description;

    [Header("Reward")]
    public float reward;

    [Header("Condition")]
    public AchievementType type;
    public float targetValue;

    [Header("Runtime")]
    public bool hidden;
}