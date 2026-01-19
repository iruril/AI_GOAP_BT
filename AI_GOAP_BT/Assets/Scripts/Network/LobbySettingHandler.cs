using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class LobbySettingHandler : MonoBehaviour
{
    public static LobbySettingHandler Instance;

    public Button CreateLobbyButton, QuitButton;
    public Toggle UsePassword, FriendlyFire, SpawnBots;
    public TMP_InputField Password;
    public TMP_Dropdown MaxPlayers, LobbyType, RespawnDelay;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    private void Start()
    {
        QuitButton.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
        });

        UsePassword.onValueChanged.AddListener((value) =>
        {
            Password.interactable = value;
        });

        CreateLobbyButton.onClick.AddListener(() =>
        {
            SteamLobby.Instance.HostLobby(GetLobbyOptions());
        });

        Password.interactable = false;

        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        UsePassword.isOn = false;
        Password.text = string.Empty;
        Password.interactable = false;
        MaxPlayers.value = 7; // Default to 16 players
        LobbyType.value = 0; // Default to Public
        SpawnBots.isOn = true;
        FriendlyFire.isOn = false;
        RespawnDelay.value = 2; // Default to 5 seconds
    }

    public SteamLobby.LobbyCreateOptions GetLobbyOptions()
    {
        var options = new SteamLobby.LobbyCreateOptions();

        bool usePassword = UsePassword.isOn && !string.IsNullOrWhiteSpace(Password.text);
        string password = usePassword ? Password.text : string.Empty;

        options.UsePassword = usePassword;
        options.Password = password;

        if (!int.TryParse(MaxPlayers.options[MaxPlayers.value].text, out var maxPlayers))
            maxPlayers = 16;

        options.MaxPlayers = maxPlayers;

        switch (LobbyType.options[LobbyType.value].text)
        {
            case "Public":
                options.lobbyVisibility = SteamLobby.LobbyVisibility.Public;
                break;
            case "Friends Only":
                options.lobbyVisibility = SteamLobby.LobbyVisibility.FriendsOnly;
                break;
            case "Private":
                options.lobbyVisibility = SteamLobby.LobbyVisibility.Private;
                break;
            default:
                options.lobbyVisibility = SteamLobby.LobbyVisibility.Public;
                break;
        }

        options.SpawnBots = SpawnBots.isOn;
        options.FriendlyFire = FriendlyFire.isOn;

        if (!float.TryParse(RespawnDelay.options[RespawnDelay.value].text, out var delay))
            delay = 5f;

        options.RespawnDelay = delay;

        return options;
    }
}
