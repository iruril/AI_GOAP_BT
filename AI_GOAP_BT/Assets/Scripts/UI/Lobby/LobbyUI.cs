using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI Instance;

    public TeamListPanel BlueTeamList, RedTeamList;
    public ManageListPanel ManageList;
    public Button ReadyButton, ExitButton, ManageButton, StartButton;

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
            StartButton.onClick.AddListener(() =>
            {
                var rm = NetworkManager.singleton as RoomManager;
                rm?.StartGame();
            });
            StartButton.interactable = false;
        }
        else
        {
            ManageButton.gameObject.SetActive(false);
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
}
