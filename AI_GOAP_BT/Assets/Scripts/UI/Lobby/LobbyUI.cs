using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI Instance;

    public TeamListPanel BlueTeamList, RedTeamList;
    public ManageListPanel ManageList;
    public Button ReadyButton, ExitButton, ManageButton;

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
        Init();
    }

    private void Init()
    {
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
            if (NetworkClient.active && NetworkServer.active)
            {
                NetworkManager.singleton.StopHost();
            }
            else if (NetworkClient.active)
            {
                NetworkManager.singleton.StopClient();
            }
        });
        ManageButton.onClick.AddListener(ToggleManagePanel);

        BlueTeamList?.JoinButton.onClick.AddListener(() =>
        {
            if (NetworkClient.localPlayer != null)
            {
                var localPlayer = NetworkClient.localPlayer.GetComponent<LobbyPlayer>();
                localPlayer.CmdTeamChangeTeam(Team.Blue);
                PreviewActor.Instance?.UpdatePreview("Blue", "MPX");
            }
        });
        RedTeamList?.JoinButton.onClick.AddListener(() =>
        {
            if (NetworkClient.localPlayer != null)
            {
                var localPlayer = NetworkClient.localPlayer.GetComponent<LobbyPlayer>();
                localPlayer.CmdTeamChangeTeam(Team.Red);
                PreviewActor.Instance?.UpdatePreview("Red", "AK-12");
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
