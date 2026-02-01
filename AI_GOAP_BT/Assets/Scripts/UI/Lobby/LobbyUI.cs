using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI Instance;

    public TeamListPanel BlueTeamList, RedTeamList;
    public ManageListPanel ManageList;
    public FriendListPanel FriendListPanel;
    public Button ReadyButton, ExitButton, ManageButton, StartButton, InviteButton, UpdateLobbyButton;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        StartCoroutine(Init());
    }

    private IEnumerator Init()
    {
        yield return new WaitUntil(() => NetworkClient.active); 
        bool isHost = NetworkClient.active && NetworkServer.active;

        ReadyButton.onClick.AddListener(() =>
        {
            if (NetworkClient.localPlayer != null)
            {
                var localPlayer = NetworkClient.localPlayer.GetComponent<LobbyPlayer>();
                localPlayer.CmdChangeReadyState(!localPlayer.readyToBegin);
            }
        });
        ExitButton.onClick.AddListener(() =>
        {
            SteamLobby.Instance?.LeaveLobby();
        });

        if (isHost)
        {
            ManageButton.onClick.AddListener(ToggleManagePanel);
            InviteButton.onClick.AddListener(ToggleFriendListPanel);
            StartButton.onClick.AddListener(() =>
            {
                var rm = NetworkManager.singleton as RoomManager;
                rm?.StartGame();
            });
            StartButton.interactable = false;
            UpdateLobbyButton.onClick.AddListener(OpenLobbySetting);
        }
        else
        {
            ManageButton.gameObject.SetActive(false);
            InviteButton.gameObject.SetActive(false);
            StartButton.gameObject.SetActive(false);
        }

        BlueTeamList?.JoinButton.onClick.AddListener(() =>
        {
            if (NetworkClient.localPlayer != null)
            {
                var localPlayer = NetworkClient.localPlayer.GetComponent<LobbyPlayer>();
                localPlayer.CmdTeamChangeTeam(Team.Blue);
            }
        });
        RedTeamList?.JoinButton.onClick.AddListener(() =>
        {
            if (NetworkClient.localPlayer != null)
            {
                var localPlayer = NetworkClient.localPlayer.GetComponent<LobbyPlayer>();
                localPlayer.CmdTeamChangeTeam(Team.Red);
            }
        });
    }

    private void ToggleFriendListPanel()
    {
        if (FriendListPanel.gameObject.activeSelf)
            FriendListPanel.DisablePanel();
        else
            FriendListPanel.EnablePanel();
    }

    private void ToggleManagePanel()
    {
        if (ManageList.gameObject.activeSelf)
            ManageList.DisablePanel();
        else
            ManageList.EnablePanel();
    }

    public void RefreshNickname(uint netId, string newName)
    {
        BlueTeamList?.ModifyNickname(netId, newName);
        RedTeamList?.ModifyNickname(netId, newName);
        ManageList?.ModifyNickname(netId, newName);
    }

    public void OpenLobbySetting()
    {
        if (SteamManager.Initialized)
        {
            if (SteamLobby.Instance == null)
            {
                Debug.LogError("SteamLobby not found!");
                return;
            }

            if (LobbySettingHandler.Instance == null)
            {
                Debug.LogError("LobbySettingHandler not found!");
                return;
            }

            if (LobbySettingHandler.Instance.gameObject.activeSelf) 
                LobbySettingHandler.Instance?.gameObject.SetActive(false);
            else
                LobbySettingHandler.Instance?.gameObject.SetActive(true);
            return;
        }
    }
}
