using System.Collections.Generic;
using UnityEngine;

public class InGameUI : MonoBehaviour
{
    public static InGameUI Instance { get; private set; }

    [SerializeField] private List<RectTransform> realTimeHUDGroup = new();
    [SerializeField] private RectTransform scoreBoardHUD;
    [SerializeField] private RectTransform gameWinHUD;
    [SerializeField] private RectTransform gameLoseHUD;
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
