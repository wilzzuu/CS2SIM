using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlotMachineManager : MonoBehaviour
{
    [Header("Machine")]
    public SlotMachine slotMachine;

    [Header("UI")]
    public Button spinButton;
    public GameObject slotItemPrefab;

    public Image reel1Image;
    public Image reel2Image;
    public Image reel3Image;

    public TMP_Text resultText;

    [Header("Animation")]
    public float spinDuration = 2f;
    public float spinSpeed = 0.05f;

    private bool _isSpinning;

    private void Start()
    {
        spinButton.onClick.AddListener(Spin);
    }

    public void Spin()
    {
        if (_isSpinning)
            return;

        StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        _isSpinning = true;

        spinButton.interactable = false;

        resultText.text = "";

        if (!slotMachine.Spin())
        {
            resultText.text = "Not enough balance!";
            spinButton.interactable = true;
            _isSpinning = false;
            yield break;
        }

        SlotResult result = slotMachine.LastResult;

        float timer = 0f;

        while (timer < spinDuration)
        {
            reel1Image.sprite = GetRandomSprite();
            reel2Image.sprite = GetRandomSprite();
            reel3Image.sprite = GetRandomSprite();

            timer += spinSpeed;

            yield return new WaitForSeconds(spinSpeed);
        }

        yield return StartCoroutine(StopReels(result));

        spinButton.interactable = true;
        _isSpinning = false;
    }

    private IEnumerator StopReels(SlotResult result)
    {
        reel1Image.sprite = Resources.Load<Sprite>($"ItemImages/{result.reel1.id.Remove(result.reel1.id.LastIndexOf('_'))}");
        yield return new WaitForSeconds(0.25f);

        reel2Image.sprite = Resources.Load<Sprite>($"ItemImages/{result.reel2.id.Remove(result.reel2.id.LastIndexOf('_'))}");
        yield return new WaitForSeconds(0.25f);

        reel3Image.sprite = Resources.Load<Sprite>($"ItemImages/{result.reel3.id.Remove(result.reel3.id.LastIndexOf('_'))}");

        if (result.isWin)
        {
            resultText.text =
                $"WIN!\n{result.winningItem.gun} | {result.winningItem.name}";
        }
        else
        {
            resultText.text = "Try Again!";
        }
    }

    private Sprite GetRandomSprite()
    {
        ItemData item =
            slotMachine.RandomPoolItem();

        return Resources.Load<Sprite>($"ItemImages/{item.imageName}");
    }
}