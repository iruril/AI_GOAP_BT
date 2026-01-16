using Mirror;
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

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        InputField.gameObject.SetActive(false);
    }

    public void SendChat()
    {
        if (string.IsNullOrWhiteSpace(InputField.text))
            return;

        IChatSender sender = GetLocalChatSender();
        if (sender == null)
        {
            Debug.LogWarning("Chat sender not found");
            return;
        }

        Color nameColor =
            sender.MyTeam == Team.Blue
                ? WorldManager.Instance.BlueTeamColor
                : WorldManager.Instance.RedTeamColor;

        LogManager.Instance.CmdSendChat(
            sender.Nickname,
            InputField.text,
            nameColor,
            sender.NetId
        );

        InputField.text = string.Empty;
        GameManager.GetInstance().InputMap.ExitChat();
    }

    public void PrintMsg(string sender, string message, Color teamColor, uint netId)
    {
        GameObject itemObj = Instantiate(ChatItemPrefab, ContentRect);
        ChatItem item = itemObj.GetComponent<ChatItem>();

        bool isLocal =
        NetworkClient.localPlayer != null &&
        NetworkClient.localPlayer.netId == netId;

        Color chatColor = isLocal ? Color.green : teamColor;

        item.SendMessage(sender, message, chatColor);
    }

    private IChatSender GetLocalChatSender()
    {
        if (NetworkClient.localPlayer == null)
            return null;

        return NetworkClient.localPlayer.GetComponent<IChatSender>();
    }
}
