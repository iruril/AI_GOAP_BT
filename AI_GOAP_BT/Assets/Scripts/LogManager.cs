using UnityEngine;
using Mirror;

public class LogManager : NetworkBehaviour
{
    public static LogManager Instance;

    private void Awake()
    {
        Instance = this;
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
}
