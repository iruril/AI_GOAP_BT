using System.Collections.Generic;
using UnityEngine;

public class InGameUI : MonoBehaviour
{
    public static InGameUI Instance { get; private set; }

    [SerializeField] private List<RectTransform> realTimeHUDs = new();
    [SerializeField] private RectTransform scoreBoardHUD;

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
        foreach (var hud in realTimeHUDs)
        {
            hud.gameObject.SetActive(true);
        }
    }

    public void HideRealTimeHUDs()
    {
        foreach (var hud in realTimeHUDs)
        {
            hud.gameObject.SetActive(false);
        }
    }

    public void ShowConditionalHUDs()
    {
        HideRealTimeHUDs();
        scoreBoardHUD.gameObject.SetActive(true);
    }

    public void HideConditionalHUDs()
    {
        scoreBoardHUD.gameObject.SetActive(false); 
        ShowRealTimeHUDs();
    }
}
