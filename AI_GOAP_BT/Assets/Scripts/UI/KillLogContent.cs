using TMPro;
using UnityEngine;

public class KillLogContent : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI killer;
    [SerializeField] TextMeshProUGUI victim;

    public void SetKillerContent(string text, Color color)
    {
        killer.text = text;
        killer.color = color;
    }

    public void SetVictimContent(string text, Color color)
    {
        victim.text = text;
        victim.color = color;
    }

    public void ResetContent()
    {
        killer.text = string.Empty;
        victim.text = string.Empty;

        killer.color = Color.white;
        victim.color = Color.white;
    }
}
