using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public Button CreateLobbyButton;
    public Button QuickJoinButton;
    public Button SettingsButton;

    private bool isHostMode = false;

    private void Start()
    {
        CreateLobbyButton.onClick.AddListener(CreateLobby);
        QuickJoinButton.onClick.AddListener(RandomJoin);
        SettingsButton.onClick.AddListener(() =>
        {
            if (!SettingsPanel.Instance.IsOpen) SettingsPanel.Instance.OpenSettings();
            else SettingsPanel.Instance.CloseSettings();
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

            SteamLobby.Instance.HostLobby();
            return;
        }

        if (isHostMode)
            NetworkManager.singleton.StartHost();
        else
            NetworkManager.singleton.StartClient();
    }

    private void RandomJoin()
    {
        if (!SteamManager.Initialized)
            return;

        SteamLobby.Instance.JoinRandomPublicLobby();
    }
}
