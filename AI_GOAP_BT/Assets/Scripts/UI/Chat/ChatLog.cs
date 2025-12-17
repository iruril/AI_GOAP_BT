using UnityEngine;
using UnityEngine.UI;

public class ChatLog : MonoBehaviour
{
    public static ChatLog Instance;

    public GameObject ChatItemPrefab;
    public RectTransform ContentRect;
    public ScrollRect ScrollRect;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SendMessage("Alice", "Hi!", Color.cyan);

        SendMessage(
            "Charlie",
            "This is a long chat message used for testing. " +
            "It checks whether the chat item automatically resizes " +
            "its height based on the length of the text content.",
            Color.green
        );

        SendMessage(
            "Bob_The_Great_Executioner_Of_The_World",
            "That gameplay today was pretty fun.",
            Color.yellow
        );
    }

    public void SendMessage(string sender, string message, Color color)
    {
        GameObject itemObj = Instantiate(ChatItemPrefab, ContentRect);
        ChatItem item = itemObj.GetComponent<ChatItem>();
        item.SendMessage(sender, message, color);
    }
}
