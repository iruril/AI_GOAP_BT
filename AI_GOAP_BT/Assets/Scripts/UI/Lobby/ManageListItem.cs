using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ManageListItem : MonoBehaviour
{
    public TextMeshProUGUI Nickname;
    public Button KickButton;

    public void SetNickname(string nickname)
    {
        Nickname.text = nickname;
    }
}
