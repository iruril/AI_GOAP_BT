using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthGuageHUD : MonoBehaviour
{
    public static HealthGuageHUD Instance;

    [SerializeField] private Image hPguage;
    [SerializeField] private TextMeshProUGUI nickname;

    private void Awake()
    {
        Instance = this;
    }

    public void SetNickname(string name)
    {
        nickname.text = name;
    }

    public void UpdateHP(float currentHP, float maxHP)
    {
        hPguage.fillAmount = currentHP / maxHP;
    }
}
