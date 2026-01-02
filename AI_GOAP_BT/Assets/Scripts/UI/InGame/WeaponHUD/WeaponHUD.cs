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
}
