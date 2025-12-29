using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class ManageListItem : MonoBehaviour
{
    public TextMeshProUGUI Nickname;
    public Button KickButton;
    public NetworkIdentity Identity { get; private set; }

    public void SetNickname(string nickname)
    {
        Nickname.text = nickname;
    }

    public void SetIdentity(NetworkIdentity identity)
    {
        Identity = identity;
    }
}
