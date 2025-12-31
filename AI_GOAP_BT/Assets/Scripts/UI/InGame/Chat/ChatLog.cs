using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatLog : MonoBehaviour
{
    public static ChatLog Instance;

    public GameObject ChatItemPrefab;
    public RectTransform ContentRect;
    public ScrollRect ScrollRect;
    public TMP_InputField InputField;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        InputField.gameObject.SetActive(false);
    }

    public void SendChat()
    {
        GameObject player = GameManager.GetInstance().MyPlayer;
        if (player == null)
            return;

        if (!string.IsNullOrWhiteSpace(InputField.text))
        {
            LogManager.Instance.CmdSendChat(
                player.GetComponent<Stat>().Nickname,
                InputField.text,
                Color.white
            );
        }

        InputField.text = string.Empty;
        GameManager.GetInstance().InputMap.ExitChat();
    }

    public void PrintMsg(string sender, string message, Color color)
    {
        GameObject itemObj = Instantiate(ChatItemPrefab, ContentRect);
        ChatItem item = itemObj.GetComponent<ChatItem>();
        item.SendMessage(sender, message, color);
    }
}
