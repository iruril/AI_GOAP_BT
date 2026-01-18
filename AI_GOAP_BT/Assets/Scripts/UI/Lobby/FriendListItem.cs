using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FriendListItem : MonoBehaviour
{
    [SerializeField] private RawImage avatarImage;
    [SerializeField] private TextMeshProUGUI friendNameText;
    [SerializeField] private Button inviteButton;
    public Button InviteButton => inviteButton;

    public void SetName(string friendName)
    {
        friendNameText.text = friendName;
    }

    public void SetAvatar(Texture2D avatarTexture)
    {
        avatarImage.texture = avatarTexture;
    }
}
