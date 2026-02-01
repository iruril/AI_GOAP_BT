using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI Instance;

    public TeamListPanel BlueTeamList, RedTeamList;
    public ManageListPanel ManageList;
    public FriendListPanel FriendListPanel;
    public Button ReadyButton, ExitButton, ManageButton, StartButton, InviteButton, UpdateLobbyButton;

    [Header("Gun Selection")]
    public TMP_Dropdown GunSelector; 
    public TextMeshProUGUI DamageText;
    public TextMeshProUGUI RPMText;
    public TextMeshProUGUI AmmoText;
    public TextMeshProUGUI MobilityText;

    private bool isUpdatingUI = false;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        StartCoroutine(Init()); 
        StartCoroutine(InitGunDropdown());
    }

    private IEnumerator Init()
    {
        yield return new WaitUntil(() => NetworkClient.active); 
        bool isHost = NetworkClient.active && NetworkServer.active;

        ReadyButton.onClick.AddListener(() =>
        {
            if (NetworkClient.localPlayer != null)
            {
                var localPlayer = NetworkClient.localPlayer.GetComponent<LobbyPlayer>();
                localPlayer.CmdChangeReadyState(!localPlayer.readyToBegin);
            }
        });
        ExitButton.onClick.AddListener(() =>
        {
            SteamLobby.Instance?.LeaveLobby();
        });

        if (isHost)
        {
            ManageButton.onClick.AddListener(ToggleManagePanel);
            InviteButton.onClick.AddListener(ToggleFriendListPanel);
            StartButton.onClick.AddListener(() =>
            {
                var rm = NetworkManager.singleton as RoomManager;
                rm?.StartGame();
            });
            StartButton.interactable = false;
            UpdateLobbyButton.onClick.AddListener(OpenLobbySetting);
        }
        else
        {
            ManageButton.gameObject.SetActive(false);
            InviteButton.gameObject.SetActive(false);
            StartButton.gameObject.SetActive(false);
        }

        BlueTeamList?.JoinButton.onClick.AddListener(() =>
        {
            if (NetworkClient.localPlayer != null)
            {
                var localPlayer = NetworkClient.localPlayer.GetComponent<LobbyPlayer>();
                localPlayer.CmdTeamChangeTeam(Team.Blue);
            }
        });
        RedTeamList?.JoinButton.onClick.AddListener(() =>
        {
            if (NetworkClient.localPlayer != null)
            {
                var localPlayer = NetworkClient.localPlayer.GetComponent<LobbyPlayer>();
                localPlayer.CmdTeamChangeTeam(Team.Red);
            }
        });
    }

    private IEnumerator InitGunDropdown()
    {
        yield return new WaitUntil(() => GameManager.GetInstance() != null && GameManager.GetInstance().GunListReady);

        GunSelector.ClearOptions();
        List<string> gunNames = GameManager.GetInstance().GunTable.Keys.ToList();
        GunSelector.AddOptions(gunNames);
        GunSelector.onValueChanged.AddListener(OnGunSelected);

        if (NetworkClient.localPlayer != null)
        {
            var localPlayer = NetworkClient.localPlayer.GetComponent<LobbyPlayer>();
            if (localPlayer != null)
            {
                SelectGunInDropdown(localPlayer.SelectedGunID);
            }
        }
    }

    private void ToggleFriendListPanel()
    {
        if (FriendListPanel.gameObject.activeSelf)
            FriendListPanel.DisablePanel();
        else
            FriendListPanel.EnablePanel();
    }

    private void ToggleManagePanel()
    {
        if (ManageList.gameObject.activeSelf)
            ManageList.DisablePanel();
        else
            ManageList.EnablePanel();
    }

    public void RefreshNickname(uint netId, string newName)
    {
        BlueTeamList?.ModifyNickname(netId, newName);
        RedTeamList?.ModifyNickname(netId, newName);
        ManageList?.ModifyNickname(netId, newName);
    }

    public void OpenLobbySetting()
    {
        if (SteamManager.Initialized)
        {
            if (SteamLobby.Instance == null)
            {
                Debug.LogError("SteamLobby not found!");
                return;
            }

            if (LobbySettingHandler.Instance == null)
            {
                Debug.LogError("LobbySettingHandler not found!");
                return;
            }

            if (LobbySettingHandler.Instance.gameObject.activeSelf) 
                LobbySettingHandler.Instance?.gameObject.SetActive(false);
            else
                LobbySettingHandler.Instance?.gameObject.SetActive(true);
            return;
        }
    }

    private void OnGunSelected(int index)
    {
        if (isUpdatingUI) return;

        if (NetworkClient.localPlayer != null)
        {
            string selectedGunID = GunSelector.options[index].text;

            UpdateWeaponInfo(selectedGunID);

            var localPlayer = NetworkClient.localPlayer.GetComponent<LobbyPlayer>();

            if (localPlayer != null)
            {
                localPlayer.CmdSelectGun(selectedGunID);
            }
        }
    }

    public void SelectGunInDropdown(string gunID)
    {
        if (GunSelector.options.Count == 0 || string.IsNullOrEmpty(gunID)) return;

        int index = -1;
        for (int i = 0; i < GunSelector.options.Count; i++)
        {
            if (GunSelector.options[i].text == gunID)
            {
                index = i;
                break;
            }
        }

        if (index != -1)
        {
            isUpdatingUI = true;
            GunSelector.value = index; 
            UpdateWeaponInfo(gunID);
            isUpdatingUI = false;
        }
    }

    public void UpdateWeaponInfo(string gunID)
    {
        if (!GameManager.GetInstance().GunTable.TryGetValue(gunID, out var gunData)) return;

        var info = gunData.gun.GunInfo;

        DamageText.text = $"Damage: {info.RoundDamage}";
        RPMText.text = $"RPM: {info.RPM}";
        AmmoText.text = $"Magazine: {info.MagazineCapacity}";
        MobilityText.text = $"ADS: {info.TimeToADS}s";
    }
}
