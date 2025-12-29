using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Collections;

public class RoomPlayer : NetworkRoomPlayer
{
    [SyncVar(hook = nameof(TeamChanged))]
    public Team MyTeam = Team.Blue;

    [SyncVar]
    public bool IsHost = false;

    [SyncVar]
    public string Nickname = "Nickname";

    public override void Start()
    {
        base.Start();
    }

    public override void OnStartServer()
    {
        IsHost = NetworkServer.connections.Count == 1;
    }

    public override void OnStartLocalPlayer()
    {
        if (!IsHost)
        {
            LobbyUI.Instance.ManageButton?.gameObject.SetActive(false);
        }
    }

    public override void OnClientEnterRoom()
    {
        StartCoroutine(Refresh());
    }

    public override void OnClientExitRoom()
    {
        StartCoroutine(Refresh());
    }

    [Command]
    public void CmdTeamChangeTeam(Team targetTeam)
    {
        MyTeam = targetTeam;
    }

    [Command]
    public void CmdKick(uint targetNetId)
    {
        if (!IsHost) return;

        if (NetworkServer.spawned.TryGetValue(targetNetId, out var id))
        {
            id.connectionToClient.Disconnect();
        }
    }

    public void TeamChanged(Team oldTeam, Team newTeam)
    {
        if (oldTeam == Team.Blue)
        {
            LobbyUI.Instance.BlueTeamList?.RemoveUser(netId);
            LobbyUI.Instance.RedTeamList?.AddUser(Nickname + netId.ToString(), netId);
            LobbyUI.Instance.RedTeamList?.SetReady(netId, readyToBegin);
        }
        else
        {
            LobbyUI.Instance.RedTeamList?.RemoveUser(netId);
            LobbyUI.Instance.BlueTeamList?.AddUser(Nickname + netId.ToString(), netId);
            LobbyUI.Instance.BlueTeamList?.SetReady(netId, readyToBegin);
        }
    }

    public override void ReadyStateChanged(bool oldReadyState, bool newReadyState) 
    {
        if (MyTeam == Team.Blue)
        {
            LobbyUI.Instance.BlueTeamList?.SetReady(this.netId, newReadyState);
        }
        else
        {
            LobbyUI.Instance.RedTeamList?.SetReady(this.netId, newReadyState);
        }

        if (isLocalPlayer)
        {
            LobbyUI.Instance.ReadyButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = newReadyState ? "Unready" : "Ready";
        }
    }

    IEnumerator Refresh()
    {
        yield return null;
        RefreshPanels();
    }

    private void RefreshPanels()
    {
        LobbyUI.Instance.BlueTeamList?.ClearPanel();
        LobbyUI.Instance.RedTeamList?.ClearPanel();

        if (IsHost && isLocalPlayer)
        {
            LobbyUI.Instance.ManageList?.ClearPanel();
        }

        foreach (var user in NetworkClient.spawned)
        {
            uint netId = user.Key;
            RoomPlayer roomPlayer = user.Value.GetComponent<RoomPlayer>(); 

            if (roomPlayer == null) continue;

            if (roomPlayer.MyTeam == Team.Blue)
            {
                LobbyUI.Instance.BlueTeamList?.AddUser(roomPlayer.Nickname + netId.ToString(), netId);
                LobbyUI.Instance.BlueTeamList?.SetReady(netId, roomPlayer.readyToBegin);
            }
            else
            {
                LobbyUI.Instance.RedTeamList?.AddUser(roomPlayer.Nickname + netId.ToString(), netId);
                LobbyUI.Instance.RedTeamList?.SetReady(netId, roomPlayer.readyToBegin);
            }

            if (isServer)
            {
                LobbyUI.Instance.ManageList?.AddUser(roomPlayer.Nickname + netId.ToString(), netId);
            }
        }
    }
}
