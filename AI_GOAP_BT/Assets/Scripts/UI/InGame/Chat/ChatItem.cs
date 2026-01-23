using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatItem : MonoBehaviour
{
    public RectTransform ItemRect;
    public TextMeshProUGUI Chat;

    public void SendMessage(string sender, string message, Color nicknameColor)
    {
        string nickColor = ColorUtility.ToHtmlStringRGBA(nicknameColor);
        message = message.Replace("<", "&lt;").Replace(">", "&gt;");

        Chat.text = $"<color=#{nickColor}>{sender} </color>" + $"<color=#FFFFFF>: {message}</color>";
    }
}
