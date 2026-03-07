using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponHUD : MonoBehaviour
{
    public static WeaponHUD Instance;

    [SerializeField] RawImage gunImage;
    [SerializeField] TextMeshProUGUI gunName;
    [SerializeField] TextMeshProUGUI fireMode;
    [SerializeField] TextMeshProUGUI currentRound;
    [SerializeField] TextMeshProUGUI maxRound;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void OnGunChanged(string gunName, int maxRound)
    {
        Texture2D tex = Resources.Load<Texture2D>("Images/" + gunName);
        if (tex != null) this.gunImage.texture = tex;
        this.gunName.text = gunName;
        this.maxRound.text = maxRound.ToString();
    }

    public void OnRoundChanged(int newRound)
    {
        currentRound.text = newRound.ToString();
    }

    public void OnSelectorChanged(FireMode fireMode)
    {
        switch (fireMode)
        {
            case FireMode.Single:
                this.fireMode.text = "Selector : Single";
                break;
            case FireMode.Auto:
                this.fireMode.text = "Selector : Auto";
                break;
            case FireMode.Burst:
                this.fireMode.text = "Selector : Burst";
                break;
        }
    }
}
