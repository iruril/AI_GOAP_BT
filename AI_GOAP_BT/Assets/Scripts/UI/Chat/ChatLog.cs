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

    }

    public void SendChat()
    {
        if (string.IsNullOrWhiteSpace(InputField.text))
            return;

        LogManager.Instance.CmdSendChat("TESTER", InputField.text, Color.green);
        InputField.text = string.Empty;
    }

    public void PrintMsg(string sender, string message, Color color)
    {
        GameObject itemObj = Instantiate(ChatItemPrefab, ContentRect);
        ChatItem item = itemObj.GetComponent<ChatItem>();
        item.SendMessage(sender, message, color);
    }
}
