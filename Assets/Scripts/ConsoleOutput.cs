using UnityEngine;
using UnityEngine.UI;

public class ConsoleOutput : MonoBehaviour
{
    public static ConsoleOutput Instance;

    [SerializeField] private Transform contentParent;
    [SerializeField] private ConsoleLine linePrefab;
    [SerializeField] private ScrollRect scrollRect;

    private void Awake()
    {
        Instance = this;
    }

    public void AddLine(string text)
    {
        ConsoleLine line =
            Instantiate(linePrefab, contentParent);

        line.SetText(text);

        Canvas.ForceUpdateCanvases();

        scrollRect.verticalNormalizedPosition = 0f;
    }
}