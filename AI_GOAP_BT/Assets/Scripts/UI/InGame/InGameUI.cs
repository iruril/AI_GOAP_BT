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
    [SerializeField] private float hitMarkFadeTime = 0.08f;

    private CoroutineHandle hitmarkHandle;

    public RectTransform GameWinHUD { get => gameWinHUD;}
    public RectTransform GameLoseHUD { get => gameLoseHUD;}

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        gameWinHUD.gameObject.SetActive(false);
        gameLoseHUD.gameObject.SetActive(false);

        Color startColor = hitMark.color;
        startColor.a = 0f;
        hitMark.color = startColor;
    }

    private void OnDestroy()
    {
        Timing.KillCoroutines(hitmarkHandle);
        Instance = null;
    }

    public void PlayHitMark()
    {
        if (hitMark == null) return;

        Timing.KillCoroutines(hitmarkHandle);

        hitmarkHandle = Timing.RunCoroutine(FadeHitMark());
    }

    private IEnumerator<float> FadeHitMark()
    {
        float elapsed = 0f;

        Color fadeColor = hitMark.color;
        fadeColor.a = 1f;
        hitMark.color = fadeColor;

        while (elapsed < hitMarkFadeTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / hitMarkFadeTime);
            float alpha = 1f - Mathf.Pow(t, 3f);

            fadeColor.a = alpha;
            hitMark.color = fadeColor;

            yield return Timing.WaitForOneFrame;
        }

        Color endColor = hitMark.color;
        endColor.a = 0f;
        hitMark.color = endColor;
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
