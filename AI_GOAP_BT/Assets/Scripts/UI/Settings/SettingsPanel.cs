using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    public static SettingsPanel Instance;

    [SerializeField]
    private Slider mouseSensitivitySlider;
    [SerializeField]
    private TextMeshProUGUI mouseSensitivityValueText;
    [SerializeField]
    private Slider gamepadSensitivitySlider;
    [SerializeField]
    private TextMeshProUGUI gamepadSensitivityValueText;
    [SerializeField]
    private Button exitButton;
    [SerializeField]
    private Button closeButton; 
    
    public bool IsOpen => gameObject.activeSelf;

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
        mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
        gamepadSensitivitySlider.onValueChanged.AddListener(OnGamepadSensitivityChanged);
        closeButton.onClick.AddListener(CloseSettings);
        exitButton.onClick.AddListener(Exit);

        gameObject.SetActive(false);
    }

    private void OnMouseSensitivityChanged(float value)
    {
        GameManager.GetInstance().InputMap.SensitivityOnMouse = value;
        mouseSensitivityValueText.text = value.ToString();
    }

    private void OnGamepadSensitivityChanged(float value)
    {
        GameManager.GetInstance().InputMap.SensitivityOnGamepad = value;
        gamepadSensitivityValueText.text = value.ToString();
    }

    private void SetMouseSliderValue(float value)
    {
        mouseSensitivitySlider.value = value;
        mouseSensitivityValueText.text = value.ToString();
    }

    private void SetGamepadSliderValue(float value)
    {
        gamepadSensitivitySlider.value = value;
        gamepadSensitivityValueText.text = value.ToString();
    }

    public void OpenSettings()
    {
        gameObject.SetActive(true);
        SetMouseSliderValue(GameManager.GetInstance().InputMap.SensitivityOnMouse);
        SetGamepadSliderValue(GameManager.GetInstance().InputMap.SensitivityOnGamepad);
    }

    public void CloseSettings()
    {
        GameManager.GetInstance().Settings.SaveFile();
        gameObject.SetActive(false);

        if (GameManager.GetInstance().IsGameplayScene)
        {
            GameManager.GetInstance().InputMap.LockCursor(true);
        }
    }

    private void Exit()
    {
        var manager = NetworkManager.singleton as RoomManager;

        bool isHost = NetworkServer.active;
        bool isClient = NetworkClient.active && !NetworkServer.active;

        if (isHost)
        {
            if (GameManager.GetInstance().IsGameplayScene)
            {
                manager.ServerChangeScene(manager.RoomScene);
                return;
            }
            SteamLobby.Instance.LeaveLobby();
            SceneManager.LoadScene("MainMenu");
            return;
        }

        if (isClient)
        {
            SteamLobby.Instance.LeaveLobby();
            SceneManager.LoadScene("MainMenu");
            return;
        }

        Application.Quit();
    }
}
