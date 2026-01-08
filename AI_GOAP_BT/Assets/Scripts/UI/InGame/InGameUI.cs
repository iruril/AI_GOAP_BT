using System.Collections.Generic;
using UnityEngine;

public class InGameUI : MonoBehaviour
{
    public static InGameUI Instance { get; private set; }

    [SerializeField] private List<RectTransform> realTimeHUDGroup = new();
    [SerializeField] private RectTransform scoreBoardHUD;
    [SerializeField] private RectTransform gameoverHUD;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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

    public void ShowGameOverHUD()
    {
        scoreBoardHUD.gameObject.SetActive(false);
        HideRealTimeHUDs();
        gameoverHUD.gameObject.SetActive(true);
    }
}
