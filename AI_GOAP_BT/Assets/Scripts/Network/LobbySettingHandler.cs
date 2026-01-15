using UnityEngine;
using TMPro;
using UnityEngine.UI;

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
        return new SteamLobby.LobbyCreateOptions
        {
            UsePassword = UsePassword.isOn,
            Password = UsePassword.isOn ? Password.text : string.Empty,
            MaxPlayers = int.Parse(MaxPlayers.options[MaxPlayers.value].text),
            lobbyVisibility = (SteamLobby.LobbyVisibility)LobbyType.value,
            SpawnBots = SpawnBots.isOn,
            FriendlyFire = FriendlyFire.isOn,
            RespawnDelay = float.Parse(RespawnDelay.options[RespawnDelay.value].text)
        };
    }
}
