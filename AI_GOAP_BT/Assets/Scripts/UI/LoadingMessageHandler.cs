using UnityEngine;
using TMPro;
using UnityEngine.UI;
using AYellowpaper.SerializedCollections;
using MEC;
using System.Collections.Generic;

public class LoadingMessageHandler : MonoBehaviour
{
    public TextMeshProUGUI Status;
    public Button QuitButton;

    [SerializeField] private float delayTime = 1.5f;

    [SerializedDictionary("JoinResult", "Output")]
    [SerializeField] private SerializedDictionary<SteamLobby.JoinResult, string> joinResults = new();

    CoroutineHandle popUpHandle;

    private const string DEF_MESSAGE = "Connecting...";

    void Start()
    {
        QuitButton.onClick.AddListener(
            () => 
            { 
                SteamLobby.Instance?.CancelJoining();
                gameObject.SetActive(false);
            }
        );

        SteamLobby.Instance.OnJoiningStateChanged += UpdatePanel;
        SteamLobby.Instance.OnJoinResult += UpdateJoinResult;

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        SteamLobby.Instance.OnJoiningStateChanged -= UpdatePanel;
        SteamLobby.Instance.OnJoinResult -= UpdateJoinResult;

        Timing.KillCoroutines(popUpHandle);
    }

    private void UpdatePanel(bool popUp)
    {
        if (popUp)
        {
            Timing.KillCoroutines(popUpHandle);
            Status.text = DEF_MESSAGE;
            gameObject.SetActive(popUp);
            QuitButton.gameObject.SetActive(true);

            SettingsPanel.Instance?.CloseSettings();
            LobbyBrowser.Instance?.CloseBrowser();
            LobbySettingHandler.Instance?.gameObject.SetActive(false);
            LobbyUI.Instance?.ManageList.DisablePanel();
            LobbyUI.Instance?.FriendListPanel.DisablePanel();

            GameManager.GetInstance().InputMap.CurrentUIState = Player.Input.UIState.Loading;
        }
        else
        {
            popUpHandle = Timing.RunCoroutine(DelayedClose());

            GameManager.GetInstance().InputMap.CurrentUIState = Player.Input.UIState.None;
        }
    }

    private void UpdateJoinResult(SteamLobby.JoinResult result)
    {
        if (!gameObject.activeSelf) return;

        Status.text = joinResults[result];
        if(result == SteamLobby.JoinResult.Success 
            || result == SteamLobby.JoinResult.Canceled)
            QuitButton.gameObject.SetActive(false);
    }

    private IEnumerator<float> DelayedClose()
    {
        yield return Timing.WaitForSeconds(delayTime);

        gameObject.SetActive(false);
    }
}
