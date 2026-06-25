using TMPro;
using UnityEngine;

public class ConsoleLine : MonoBehaviour
{
    [SerializeField] private TMP_Text textField;

    public void SetText(string text)
    {
        textField.text = text;
    }
}