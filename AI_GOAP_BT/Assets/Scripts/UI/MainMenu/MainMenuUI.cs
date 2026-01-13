using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public TMP_InputField NicknameInput;
    public TMP_InputField RoomAddressInput;
    public TMP_InputField RoomPortInput;
    public Toggle HostModeToggle;
    public Button StartButton;

    private bool isHostMode = false;

    private void Start()
    {
        isHostMode = HostModeToggle.isOn;
        RoomAddressInput.text = NetworkManager.singleton.networkAddress; 
        if (Transport.active is PortTransport portTransport)
        {
            RoomPortInput.text = portTransport.Port.ToString();
        }
        LocalPlayerSettings.Nickname = NicknameInput.text;

        HostModeToggle.onValueChanged.AddListener(value => isHostMode = value);
        RoomPortInput.onEndEdit.AddListener(OnRoomPortInputEndEdit);
        RoomAddressInput.onEndEdit.AddListener(value => NetworkManager.singleton.networkAddress = value);
        NicknameInput.onEndEdit.AddListener(value => LocalPlayerSettings.Nickname = value);
        StartButton.onClick.AddListener(OnStartButtonClicked);
    }

    private void OnRoomPortInputEndEdit(string value)
    {
        if (ushort.TryParse(value, out ushort port))
        {
            if (Transport.active is PortTransport pt)
                pt.Port = port;
        }
    }

    public void OnStartButtonClicked()
    {

        if (string.IsNullOrWhiteSpace(LocalPlayerSettings.Nickname))
        {
            LocalPlayerSettings.Nickname = "Player" + UnityEngine.Random.Range(0, 999).ToString("D3");
        }

        if (SteamManager.Initialized)
        {
            if (SteamLobby.Instance == null)
            {
                Debug.LogError("SteamLobby not found!");
                return;
            }

            SteamLobby.Instance.HostLobby();
            return;
        }

        if (isHostMode)
            NetworkManager.singleton.StartHost();
        else
            NetworkManager.singleton.StartClient();
    }
}
