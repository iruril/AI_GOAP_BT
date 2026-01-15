using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyBrowserItem : MonoBehaviour
{
    public TextMeshProUGUI LobbyName, MaxPlayers, CurrentPlayers, LobbyType;
    public Button JoinLobbyButton;
    private ulong lobbyID;

    private void Start()
    {
        JoinLobbyButton.onClick.AddListener(JoinLobby);
    }

    public void SetLobbyName(string name)
    {
        LobbyName.text = name;
    }

    public void SetMaxPlayers(int maxPlayers)
    {
        MaxPlayers.text = maxPlayers.ToString();
    }

    public void SetCurrentPlayers(int currentPlayers)
    {
        CurrentPlayers.text = currentPlayers.ToString();
    }

    public void SetLobbyType(SteamLobby.LobbyVisibility type)
    {
        switch (type)
        {
            case SteamLobby.LobbyVisibility.Public:
                LobbyType.text = "Public";
                break;
            case SteamLobby.LobbyVisibility.FriendsOnly:
                LobbyType.text = "Friends Only";
                break;
            default:
                LobbyType.text = "Private";
                break;
        }
    }

    public void SetLobbyInfo(ulong lobbyID)
    {
        this.lobbyID = lobbyID;
        CSteamID lobbyCSteamID = new CSteamID(lobbyID);

        string lobbyName = SteamMatchmaking.GetLobbyData(lobbyCSteamID, "name");
        SetLobbyName(lobbyName);

        int maxPlayers = 0;
        int.TryParse(
            SteamMatchmaking.GetLobbyData(lobbyCSteamID, "maxPlayers"),
            out maxPlayers
        );

        int currentPlayers = SteamMatchmaking.GetNumLobbyMembers(lobbyCSteamID);
        if (currentPlayers >= maxPlayers)
        {
            Destroy(gameObject);
            return;
        }

        SetMaxPlayers(maxPlayers);
        SetCurrentPlayers(currentPlayers);

        string visibilityStr = SteamMatchmaking.GetLobbyData(lobbyCSteamID, "visibility");
        switch (visibilityStr)
        {
            case "public":
                SetLobbyType(SteamLobby.LobbyVisibility.Public);
                break;

            case "friends":
                SetLobbyType(SteamLobby.LobbyVisibility.FriendsOnly);
                break;

            case "private":
                SetLobbyType(SteamLobby.LobbyVisibility.Private);
                break;

            default:
                SetLobbyType(SteamLobby.LobbyVisibility.Public);
                break;
        }
    }

    public void JoinLobby()
    {
        SteamLobby.Instance.JoinLobby(lobbyID);
    }
}
