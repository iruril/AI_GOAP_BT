using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class LobbySettingHandler : MonoBehaviour
{
    public static LobbySettingHandler Instance;

    public Button CreateLobbyButton, QuitButton;
    public Toggle UsePassword, FriendlyFire, SpawnBots;
    public TMP_InputField Password;
    public TMP_Dropdown MaxPlayers, LobbyType, RespawnDelay;
    public TextMeshProUGUI CreateButtonText;

    private bool isEditMode = false;

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

        CreateLobbyButton.onClick.AddListener(OnConfirmButtonClicked);

        Password.interactable = false;
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (SteamLobby.Instance != null && SteamLobby.Instance.CurrentLobbyID != 0)
        {
            isEditMode = true;
            if (CreateButtonText) CreateButtonText.text = "Update Lobby";
            InitializeUIFromCurrentSettings();
        }
        else
        {
            isEditMode = false;
            if (CreateButtonText) CreateButtonText.text = "Create Lobby";
            ResetToDefaultUI();
        }
    }

    private void OnConfirmButtonClicked()
    {
        var options = GetLobbyOptions();

        if (isEditMode)
        {
            SteamLobby.Instance.UpdateLobbySettings(options);
            gameObject.SetActive(false);
        }
        else
        {
            SteamLobby.Instance.HostLobby(options);
        }
    }

    public SteamLobby.LobbyCreateOptions GetLobbyOptions()
    {
        var options = new SteamLobby.LobbyCreateOptions();

        bool usePassword = UsePassword.isOn && !string.IsNullOrWhiteSpace(Password.text);
        options.UsePassword = usePassword;
        options.Password = usePassword ? Password.text : string.Empty;

        if (!int.TryParse(MaxPlayers.options[MaxPlayers.value].text, out var maxPlayers))
            maxPlayers = 16;
        options.MaxPlayers = maxPlayers;

        options.SpawnBots = SpawnBots.isOn;
        options.FriendlyFire = FriendlyFire.isOn;

        if (!float.TryParse(RespawnDelay.options[RespawnDelay.value].text, out var delay))
            delay = 5f;
        options.RespawnDelay = delay;

        string selectedType = LobbyType.options[LobbyType.value].text;
        switch (selectedType)
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

        return options;
    }

    private void ResetToDefaultUI()
    {
        UsePassword.isOn = false;
        Password.text = string.Empty;
        Password.interactable = false;
        MaxPlayers.value = 0; // Dropdown ÀÎµ¦½º¿¡ ¸ÂÃç Á¶Á¤ ÇÊ¿ä
        SpawnBots.isOn = true;
        FriendlyFire.isOn = false;
        RespawnDelay.value = 0; // Dropdown ÀÎµ¦½º

        SetupLobbyTypeDropdown(allowPrivate: true);
        LobbyType.value = 0; // Public Default
    }

    private void InitializeUIFromCurrentSettings()
    {
        var currentOptions = SteamLobby.Instance.CurrentLobbyOptions;

        UsePassword.isOn = currentOptions.UsePassword;
        Password.text = currentOptions.Password;
        Password.interactable = currentOptions.UsePassword;

        SetDropdownValueByText(MaxPlayers, currentOptions.MaxPlayers.ToString());

        SpawnBots.isOn = currentOptions.SpawnBots;
        FriendlyFire.isOn = currentOptions.FriendlyFire;
        SetDropdownValueByText(RespawnDelay, currentOptions.RespawnDelay.ToString());

        bool isCurrentlyPrivate = (currentOptions.lobbyVisibility == SteamLobby.LobbyVisibility.Private);

        SetupLobbyTypeDropdown(allowPrivate: isCurrentlyPrivate);

        string currentTypeString = currentOptions.lobbyVisibility switch
        {
            SteamLobby.LobbyVisibility.Public => "Public",
            SteamLobby.LobbyVisibility.FriendsOnly => "Friends Only",
            _ => "Private"
        };
        SetDropdownValueByText(LobbyType, currentTypeString);
    }

    private void SetupLobbyTypeDropdown(bool allowPrivate)
    {
        LobbyType.ClearOptions();
        List<string> options = new List<string> { "Public", "Friends Only" };

        if (allowPrivate)
        {
            options.Add("Private");
        }

        LobbyType.AddOptions(options);
    }

    private void SetDropdownValueByText(TMP_Dropdown dropdown, string text)
    {
        for (int i = 0; i < dropdown.options.Count; i++)
        {
            if (dropdown.options[i].text == text)
            {
                dropdown.value = i;
                return;
            }
        }
    }
}
