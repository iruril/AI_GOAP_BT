using Mirror;
using TMPro;
using UnityEngine;

public class ScoreboardItem : MonoBehaviour
{
    public TextMeshProUGUI Nickname, Num, Kills, Deaths, Assists;
    public int KillsValue { get; private set; }
    public int AssistsValue { get; private set; }
    public int DeathsValue { get; private set; }
    public string NicknameValue { get; private set; }
    public int Score => KillsValue * 100 + AssistsValue * 25;

    public void Init(uint netId, bool isBlue)
    {
        if (NetworkClient.localPlayer != null &&
            NetworkClient.localPlayer.netId == netId)
        {
            Nickname.color = Color.cyan;
            Num.color = Color.cyan;
            Kills.color = Color.cyan;
            Deaths.color = Color.cyan;
            Assists.color = Color.cyan;
        }
        else
        {
            Nickname.color = isBlue ? WorldManager.Instance.BlueTeamColor : WorldManager.Instance.RedTeamColor;
            Num.color = Color.white;
            Kills.color = Color.white;
            Deaths.color = Color.white;
            Assists.color = Color.white;
        }
    }

    public void SetNickname(string nickname)
    {
        NicknameValue = nickname;
        Nickname.text = nickname;
    }

    public void SetNumber(int num)
    {
        string number = num.ToString("D2") + ".";
        Num.text = number;
    }

    public void SetKills(int kills)
    {
        KillsValue = kills;
        Kills.text = kills.ToString();
    }

    public void SetDeaths(int deaths)
    {
        DeathsValue = deaths;
        Deaths.text = deaths.ToString();
    }

    public void SetAssists(int assists)
    {
        AssistsValue = assists;
        Assists.text = assists.ToString();
    }
}
