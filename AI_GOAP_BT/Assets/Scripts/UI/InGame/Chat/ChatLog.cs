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
        if (!string.IsNullOrWhiteSpace(InputField.text))
        {
            Color nameColor;
            var stat = GameManager.GetInstance().MyPlayer.GetComponent<Stat>();

            if (stat == null)
            {
                Debug.LogError("Stat component not found on player!");
                return;
            }

            if (stat.MyTeam == Team.Blue)
                nameColor = WorldManager.Instance.BlueTeamColor;
            else if (stat.MyTeam == Team.Red)
                nameColor = WorldManager.Instance.RedTeamColor;
            else
                nameColor = Color.white;

            LogManager.Instance.CmdSendChat(
                stat.Nickname,
                InputField.text,
                nameColor,
                GameManager.GetInstance().MyNetId
            );
        }

        InputField.text = string.Empty;
        GameManager.GetInstance().InputMap.ExitChat();
    }

    public void PrintMsg(string sender, string message, Color color, uint netId)
    {
        GameObject itemObj = Instantiate(ChatItemPrefab, ContentRect);
        ChatItem item = itemObj.GetComponent<ChatItem>();

        Color chatColor = 
            netId == GameManager.GetInstance().MyNetId ?
            Color.green :
            color;
        item.SendMessage(sender, message, chatColor);
    }
}
