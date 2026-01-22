using MEC;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InGameUI : MonoBehaviour
{
    public static InGameUI Instance { get; private set; }

    [SerializeField] private List<RectTransform> realTimeHUDGroup = new();
    [SerializeField] private RectTransform scoreBoardHUD;
    [SerializeField] private RectTransform gameWinHUD;
    [SerializeField] private RectTransform gameLoseHUD;

    [Header("Hit Mark")]
    [SerializeField] private Image hitMark;
    [SerializeField] private Image killMark;
    [SerializeField] private float hitMarkFadeTime = 0.08f;

    [Header("Crosshair")]
    [SerializeField] private Image crossHair_L;
    [SerializeField] private Image crossHair_R;
    [SerializeField] private Image crossHair_U;
    [SerializeField] private Image crossHair_D;
    [SerializeField] private float crossHairSpreadScale = 4.0f;

    private Vector2 pos_L, pos_R, pos_U, pos_D;

    private CoroutineHandle hitmarkHandle;
    private CoroutineHandle killmarkHandle;

    public RectTransform GameWinHUD { get => gameWinHUD;}
    public RectTransform GameLoseHUD { get => gameLoseHUD;}

    private void Awake()
    {
        Instance = this;

        pos_L = crossHair_L.rectTransform.anchoredPosition;
        pos_R = crossHair_R.rectTransform.anchoredPosition;
        pos_U = crossHair_U.rectTransform.anchoredPosition;
        pos_D = crossHair_D.rectTransform.anchoredPosition;
    }

    private void Start()
    {
        gameWinHUD.gameObject.SetActive(false);
        gameLoseHUD.gameObject.SetActive(false);

        Color startColor = hitMark.color;
        startColor.a = 0f;
        hitMark.color = startColor;

        startColor = killMark.color;
        startColor.a = 0f;
        killMark.color = startColor;
    }

    private void OnDestroy()
    {
        Timing.KillCoroutines(hitmarkHandle);
        Instance = null;
    }

    public void SetCrossHairSpread(float weight)
    {
        weight = Mathf.Clamp01(weight);

        crossHair_L.rectTransform.anchoredPosition = pos_L * weight + (pos_L * crossHairSpreadScale) * (1 - weight);
        crossHair_R.rectTransform.anchoredPosition = pos_R * weight + (pos_R * crossHairSpreadScale) * (1 - weight);
        crossHair_U.rectTransform.anchoredPosition = pos_U * weight + (pos_U * crossHairSpreadScale) * (1 - weight);
        crossHair_D.rectTransform.anchoredPosition = pos_D * weight + (pos_D * crossHairSpreadScale) * (1 - weight);
    }

    public void PlayHitMark(bool isKill)
    {
        if (hitMark == null) return;

        Image mark = isKill ? killMark : hitMark;
        float fadeTime = isKill ? hitMarkFadeTime * 1.5f : hitMarkFadeTime;

        if (isKill)
        {
            Timing.KillCoroutines(killmarkHandle);
            killmarkHandle = Timing.RunCoroutine(FadeHitMark(mark, fadeTime));
        }
        else
        {
            Timing.KillCoroutines(hitmarkHandle);
            hitmarkHandle = Timing.RunCoroutine(FadeHitMark(mark, fadeTime));
        }
    }

    private IEnumerator<float> FadeHitMark(Image image, float fadeTime)
    {
        float elapsed = 0f;

        Color fadeColor = image.color;
        fadeColor.a = 1f;
        image.color = fadeColor;

        Transform target = image.transform;
        Vector3 startScale = Vector3.one;
        Vector3 endScale = Vector3.one * 1.5f;
        target.localScale = startScale;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeTime);

            float alpha = 1f - Mathf.Pow(t, 3f);
            fadeColor.a = alpha;
            image.color = fadeColor;

            target.localScale = Vector3.Lerp(startScale, endScale, t);

            yield return Timing.WaitForOneFrame;
        }

        fadeColor.a = 0f;
        image.color = fadeColor;
        target.localScale = endScale;
    }

    public void ShowRealTimeHUDs()
    {
        foreach (var hud in realTimeHUDGroup)
        {
            hud.gameObject.SetActive(true);
        }
    }

    public void HideRealTimeHUDs()
    {
        foreach (var hud in realTimeHUDGroup)
        {
            hud.gameObject.SetActive(false);
        }
    }

    public void ShowScoreboardHUD()
    {
        HideRealTimeHUDs();
        scoreBoardHUD.gameObject.SetActive(true);
    }

    public void HideScoreboardHUD()
    {
        scoreBoardHUD.gameObject.SetActive(false); 
        ShowRealTimeHUDs();
    }

    public void ShowGameOverHUD(Team winningTeam)
    {
        scoreBoardHUD.gameObject.SetActive(false);
        HideRealTimeHUDs();
        if (winningTeam == GameManager.GetInstance().MyPlayer.GetComponent<Stat>().MyTeam)
            gameWinHUD.gameObject.SetActive(true);
        else
            gameLoseHUD.gameObject.SetActive(true);
    }
}
