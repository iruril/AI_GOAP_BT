using TMPro;
using UnityEngine;
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
    private Button closeButton; 
    
    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
        gamepadSensitivitySlider.onValueChanged.AddListener(OnGamepadSensitivityChanged);
        closeButton.onClick.AddListener(CloseSettings);

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

    public void SetMouseSliderValue(float value)
    {
        mouseSensitivitySlider.value = value;
        mouseSensitivityValueText.text = value.ToString();
    }

    public void SetGamepadSliderValue(float value)
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
    }
}
