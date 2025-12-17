using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatItem : MonoBehaviour
{
    public RectTransform ItemRect;
    public TextMeshProUGUI Nickname, Chat;

    public void SendMessage(string sender, string messege, Color nicknameColor)
    {
        Nickname.text = sender;
        Nickname.color = nicknameColor;
        Chat.text = messege;
    }
}
