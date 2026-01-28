using TMPro;
using UnityEngine;

public class KillLogContent : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI log;

    public void SetContent(string text)
    {
        log.text = text;
    }

    public void ResetContent()
    {
        log.text = string.Empty;
        log.color = Color.white;
    }
}
