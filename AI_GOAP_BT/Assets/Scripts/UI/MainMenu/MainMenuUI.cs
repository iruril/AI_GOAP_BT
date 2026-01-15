using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public Button CreateLobbyButton;
    public Button BrowseLobbyButton;
    public Button QuickJoinButton;
    public Button SettingsButton;

    private void Start()
    {
        CreateLobbyButton.onClick.AddListener(CreateLobby);
        QuickJoinButton.onClick.AddListener(RandomJoin);
        SettingsButton.onClick.AddListener(() =>
        {
            if (SettingsPanel.Instance == null) return;

            if (!SettingsPanel.Instance.IsOpen) 
                SettingsPanel.Instance.OpenSettings();
            else 
                SettingsPanel.Instance.CloseSettings();
        });
        BrowseLobbyButton.onClick.AddListener(() =>
        {
            if (LobbyBrowser.Instance == null) return;

            if (!LobbyBrowser.Instance.IsOpen)
                LobbyBrowser.Instance.OpenBrowser();
            else
                LobbyBrowser.Instance.CloseBrowser();
        });
    }

    public void CreateLobby()
    {
        if (SteamManager.Initialized)
        {
            if (SteamLobby.Instance == null)
            {
                Debug.LogError("SteamLobby not found!");
                return;
            }

            LobbySettingHandler.Instance?.gameObject.SetActive(true);
            return;
        }
    }

    private void RandomJoin()
    {
        if (!SteamManager.Initialized)
            return;

        SteamLobby.Instance.JoinRandomPublicLobby();
    }
}
