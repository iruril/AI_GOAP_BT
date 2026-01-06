using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameSituationHUD : MonoBehaviour
{
    public static GameSituationHUD Instance = null;

    public TextMeshProUGUI blueScoreText, redScoreText;
    public Image blueScoreFill, redScoreFill;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    public void UpdateBlueScore(float current, float total)
    {
        blueScoreText.text = Mathf.CeilToInt(current).ToString("D4");
        blueScoreFill.fillAmount = current / total;
    }

    public void UpdateRedScore(float current, float total)
    {
        redScoreText.text = Mathf.CeilToInt(current).ToString("D4");
        redScoreFill.fillAmount = current / total;
    }
}
