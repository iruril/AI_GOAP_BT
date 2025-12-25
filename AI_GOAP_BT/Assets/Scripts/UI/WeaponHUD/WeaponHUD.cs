using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponHUD : MonoBehaviour
{
    public static WeaponHUD Instance;

    [SerializeField] RawImage gunImage;
    [SerializeField] TextMeshProUGUI gunName;
    [SerializeField] TextMeshProUGUI currentRound;
    [SerializeField] TextMeshProUGUI maxRound;

    private void Awake()
    {
        Instance = this;
    }

    public void OnGunChanged(RawImage gunImage, string gunName, int maxRound)
    {
        if (gunImage != null) this.gunImage = gunImage;
        this.gunName.text = gunName;
        this.maxRound.text = maxRound.ToString();
    }

    public void OnRoundChanged(int newRound)
    {
        currentRound.text = newRound.ToString();
    }
}
