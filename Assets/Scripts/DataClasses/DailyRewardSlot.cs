using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DailyRewardSlot : MonoBehaviour
{
    public int dayIndex;

    public Button claimButton;
    public TextMeshProUGUI rewardText;

    private string lockedColor = "#ca0b0b";
    private string availableColor = "#2dcb05";
    private string claimedColor = "#176304";

    public void SetLocked(TextMeshProUGUI claimButtonText)
    {
        claimButton.interactable = false;
        claimButton.image.color = Globals.GetHexColor(lockedColor);
        claimButtonText.text = "Locked";
    }

    public void SetAvailable(TextMeshProUGUI claimButtonText)
    {
        claimButton.interactable = true;
        claimButton.image.color = Globals.GetHexColor(availableColor);;
        claimButtonText.text = "Claim";
    }

    public void SetClaimed(TextMeshProUGUI claimButtonText)
    {
        claimButton.interactable = false;
        claimButton.image.color = Globals.GetHexColor(claimedColor);;
        claimButtonText.text = "Claimed";
    }
}