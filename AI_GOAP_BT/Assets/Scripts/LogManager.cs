using UnityEngine;
using Mirror;

public class LogManager : NetworkBehaviour
{
    public static LogManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    [Server]
    public void ReportKill(string killer, string victim, bool isKillerBlue, bool isVictimBlue)
    {
        RpcBroadcastKill(killer, victim, isKillerBlue, isVictimBlue);
    }

    [ClientRpc]
    void RpcBroadcastKill(string killer, string victim, bool isKillerBlue, bool isVictimBlue)
    {
        KillLogUI.Instance?.AddLog(killer, victim, isKillerBlue, isVictimBlue);
    }

    [Command(requiresAuthority = false)]
    public void CmdSendChat(string sender, string message, Color color, uint netId, bool isLocal)
    {
        RpcBroadcastChat(sender, message, color, netId, isLocal);
    }

    [ClientRpc]
    void RpcBroadcastChat(string sender, string message, Color color, uint netId, bool isLocal)
    {
        ChatLog.Instance?.PrintMsg(sender, message, color, netId, isLocal);
    }
}
