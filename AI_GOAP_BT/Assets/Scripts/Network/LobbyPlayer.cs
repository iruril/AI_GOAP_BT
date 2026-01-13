using Mirror;
using System.Collections;
using Steamworks;

public class LobbyPlayer : NetworkRoomPlayer
{
    [SyncVar(hook = nameof(TeamChanged))]
    public Team MyTeam = Team.Blue; 
    
    [SyncVar]
    public bool IsHost = false;

    [SyncVar(hook = nameof(OnNicknameChanged))]
    public string Nickname = "Nickname";

    [SyncVar(hook = nameof(OnSteamIDChanged))]
    public ulong SteamID;

    public override void Start()
    {
        base.Start();
    }

    public override void OnStartServer()
    {
        IsHost = NetworkServer.connections.Count == 1;
    }

    public override void OnClientEnterRoom()
    {
        if (isLocalPlayer)
        {
            if (SteamManager.Initialized)
            {
                CmdSetNickname(SteamFriends.GetPersonaName());
                CmdSetSteamID(SteamUser.GetSteamID().m_SteamID);
            }
            else
            {
                CmdSetNickname("Player" + UnityEngine.Random.Range(0, 999).ToString("D3"));
            }
            TeamChanged(MyTeam, MyTeam);
        }

        StartCoroutine(Refresh());
    }

    public override void OnClientExitRoom()
    {
        StartCoroutine(Refresh());
    }

    [Command]
    public void CmdSetNickname(string nickname)
    {
        Nickname = nickname;
    }

    [Command]
    public void CmdSetSteamID(ulong id)
    {
        SteamID = id;
    }

    [Command]
    public void CmdTeamChangeTeam(Team targetTeam)
    {
        MyTeam = targetTeam;
    }

    [Command]
    public void CmdKick(uint targetNetId)
    {
        if (NetworkServer.spawned.TryGetValue(targetNetId, out var id))
        {
            id.connectionToClient.Disconnect();
        }
    }

    public void OnNicknameChanged(string _, string newName)
    {
        if (!IsLobbyUIAlive()) return;

        LobbyUI.Instance?.RefreshNickname(netId, newName);
    }

    public void TeamChanged(Team oldTeam, Team newTeam)
    {
        if (!IsLobbyUIAlive()) return; 
        
        if (isLocalPlayer)
        {
            if (newTeam == Team.Blue)
            {
                PreviewActor.Instance?.UpdatePreview("Blue", "MPX");
            }
            else
            {
                PreviewActor.Instance?.UpdatePreview("Red", "AK-12");
            }
        }

        if (oldTeam == Team.Blue)
        {
            LobbyUI.Instance?.BlueTeamList?.RemoveUser(netId);
            LobbyUI.Instance?.RedTeamList?.AddUser(Nickname, netId);
            LobbyUI.Instance?.RedTeamList?.SetReady(netId, readyToBegin);
        }
        else
        {
            LobbyUI.Instance?.RedTeamList?.RemoveUser(netId);
            LobbyUI.Instance?.BlueTeamList?.AddUser(Nickname, netId);
            LobbyUI.Instance?.BlueTeamList?.SetReady(netId, readyToBegin);
        }
    }

    public override void ReadyStateChanged(bool oldReadyState, bool newReadyState)
    {
        if (!IsLobbyUIAlive()) return;

        if (MyTeam == Team.Blue)
        {
            LobbyUI.Instance?.BlueTeamList?.SetReady(this.netId, newReadyState);
        }
        else
        {
            LobbyUI.Instance?.RedTeamList?.SetReady(this.netId, newReadyState);
        }

        if (isLocalPlayer)
        {
            LobbyUI.Instance.ReadyButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = newReadyState ? "Unready" : "Ready";
        }
    }

    public void OnSteamIDChanged(ulong oldReadyState, ulong newReadyState)
    {

    }

    IEnumerator Refresh()
    {
        yield return null;
        RefreshPanels();
    }

    private void RefreshPanels()
    {
        LobbyUI.Instance?.BlueTeamList?.ClearPanel();
        LobbyUI.Instance?.RedTeamList?.ClearPanel();

        if (NetworkClient.active && NetworkServer.active && isLocalPlayer)
        {
            LobbyUI.Instance?.ManageList?.ClearPanel();
        }

        foreach (var user in NetworkClient.spawned)
        {
            uint netId = user.Key;
            LobbyPlayer roomPlayer = user.Value.GetComponent<LobbyPlayer>(); 

            if (roomPlayer == null) continue;

            if (roomPlayer.MyTeam == Team.Blue)
            {
                LobbyUI.Instance?.BlueTeamList?.AddUser(roomPlayer.Nickname, netId);
                LobbyUI.Instance?.BlueTeamList?.SetReady(netId, roomPlayer.readyToBegin);
            }
            else
            {
                LobbyUI.Instance?.RedTeamList?.AddUser(roomPlayer.Nickname, netId);
                LobbyUI.Instance?.RedTeamList?.SetReady(netId, roomPlayer.readyToBegin);
            }

            if (isServer)
            {
                LobbyUI.Instance?.ManageList?.AddUser(roomPlayer.Nickname, netId);
            }
        }
    }

    private bool IsLobbyUIAlive()
    {
        return LobbyUI.Instance != null && LobbyUI.Instance.gameObject != null;
    }
}
