using UnityEngine;
using UnityEngine.UI;

public class ConsoleOutput : MonoBehaviour
{
    public static ConsoleOutput Instance;

    [SerializeField] private Transform contentParent;
    [SerializeField] private ConsoleLine linePrefab;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private int maxLines = 200;

    private void Awake()
    {
        Instance = this;

        while (contentParent.childCount > maxLines)
        {
            Destroy(contentParent.GetChild(0).gameObject);
        }
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