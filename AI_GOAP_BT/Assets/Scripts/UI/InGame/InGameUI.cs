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

    [Header("Blood Screen")]
    public CanvasGroup bloodScreenCG;

    [Header("Damage Indicator")]
    public RectTransform[] indicatorPool;
    private CanvasGroup[] indicatorCGs;
    private int poolIndex = 0;
    private CoroutineHandle[] indicatorHandles;
    [SerializeField] private float stayTime = 1.0f;
    [SerializeField] private float fadeTime = 0.5f;

    [Header("Respawn Effect")]
    [SerializeField] private CanvasGroup respawnFlashCG;

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

        indicatorCGs = new CanvasGroup[indicatorPool.Length];
        indicatorHandles = new CoroutineHandle[indicatorPool.Length];
        for (int i = 0; i < indicatorPool.Length; i++)
        {
            indicatorCGs[i] = indicatorPool[i].GetComponent<CanvasGroup>();
            indicatorCGs[i].alpha = 0;
            indicatorPool[i].gameObject.SetActive(false);
        }
        bloodScreenCG.alpha = 0f;
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

        respawnFlashCG.alpha = 0f;
        respawnFlashCG.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        Timing.KillCoroutines(hitmarkHandle);
        Timing.KillCoroutines(killmarkHandle);
        for (int i = 0; i < indicatorHandles.Length; i++) 
            Timing.KillCoroutines(indicatorHandles[i]);
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

    public void ShowDamageIndicator(Vector3 attackerPos)
    {
        int idx = poolIndex;
        poolIndex = (poolIndex + 1) % indicatorPool.Length;

        indicatorPool[idx].gameObject.SetActive(true);

        Timing.KillCoroutines(indicatorHandles[idx]);
        indicatorHandles[idx] = Timing.RunCoroutine(FadeIndicator(idx, attackerPos));
    }

    public void UpdateBloodScreenBase(float hpRatio)
    {
        bloodScreenCG.alpha = Mathf.InverseLerp(0.7f, 0.3f, hpRatio);
    }

    private IEnumerator<float> FadeIndicator(int idx, Vector3 attackerPos)
    {
        CanvasGroup cg = indicatorCGs[idx];
        RectTransform rect = indicatorPool[idx];
        Transform camT = CameraManager.Instance.MainCam.transform;

        cg.alpha = 1f;

        float elapsed = 0f;
        float totalTime = stayTime + fadeTime;

        while (elapsed < totalTime)
        {
            elapsed += Time.deltaTime;

            Vector3 localDir = camT.InverseTransformPoint(attackerPos);
            float angle = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
            rect.localRotation = Quaternion.Euler(0, 0, -angle);

            if (elapsed > stayTime)
            {
                float t = (elapsed - stayTime) / fadeTime;
                cg.alpha = 1f - t;
            }

            yield return Timing.WaitForOneFrame;
        }

        cg.alpha = 0f;
        indicatorPool[idx].gameObject.SetActive(false);
    }

    public void PlayRespawnFlash()
    {
        Timing.RunCoroutine(RespawnFlashHandle());
    }

    private IEnumerator<float> RespawnFlashHandle()
    {
        respawnFlashCG.gameObject.SetActive(true);
        respawnFlashCG.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            respawnFlashCG.alpha = elapsed / 0.5f;
            yield return Timing.WaitForOneFrame;
        }

        DeathScreenUI.Instance?.Close();
        yield return Timing.WaitForSeconds(0.1f);

        elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            respawnFlashCG.alpha = 1f - (elapsed / 0.5f);
            yield return Timing.WaitForOneFrame;
        }

        respawnFlashCG.alpha = 0f;
        respawnFlashCG.gameObject.SetActive(false);

        ShowRealTimeHUDs();
    }

    public void ShowRealTimeHUDs()
    {
        if (DeathScreenUI.Instance.gameObject.activeSelf) return;

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
