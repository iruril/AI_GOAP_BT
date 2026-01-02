using Mirror;
using TMPro;
using UnityEngine;

public class PlayerListItem : MonoBehaviour
{
    public TextMeshProUGUI Nickname, Num, Status;

    public void Init(uint netId)
    {
        if (NetworkClient.localPlayer != null &&
            NetworkClient.localPlayer.netId == netId)
        {
            Nickname.color = Color.cyan;
        }
        else
        {
            Nickname.color = Color.white;
        }
    }

    public void SetNickname(string nickname)
    {
        Nickname.text = nickname;
    }

    public void SetNumber(int num)
    {
        string number = num.ToString("D2") + ".";
        Num.text = number;
    }

    public void SetReady(bool ready)
    {
        Status.color = ready ? Color.green : Color.yellow;
        Status.text = ready ? "<Ready>" : "<Not Ready>";
    }
}
