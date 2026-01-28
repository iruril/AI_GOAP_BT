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
    public void ReportKill(string killer, string victim, bool isKillerBlue, bool isVictimBlue, bool isHeadshot, string gunName)
    {
        RpcBroadcastKill(killer, victim, isKillerBlue, isVictimBlue, isHeadshot, gunName);
    }

    [ClientRpc]
    void RpcBroadcastKill(string killer, string victim, bool isKillerBlue, bool isVictimBlue, bool isHeadshot, string gunName)
    {
        KillLogUI.Instance?.AddLog(killer, victim, isKillerBlue, isVictimBlue, isHeadshot, gunName);
    }

    [Command(requiresAuthority = false)]
    public void CmdSendChat(string sender, string message, Color color, uint netId)
    {
        RpcBroadcastChat(sender, message, color, netId);
    }

    [ClientRpc]
    void RpcBroadcastChat(string sender, string message, Color color, uint netId)
    {
        ChatLog.Instance?.PrintMsg(sender, message, color, netId);
    }
}
