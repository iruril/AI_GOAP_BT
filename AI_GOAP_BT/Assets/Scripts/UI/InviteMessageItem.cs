using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InviteMessageItem : MonoBehaviour
{
    public Button AcceptButton;
    public Button DeclineButton;
    public TextMeshProUGUI InviterName;
    public RawImage InviterAvatar;

    private ulong inviterId;
    public ulong InviterId => inviterId;

    private void Start()
    {
        DeclineButton.onClick.AddListener(() =>
        {
            InviteMessageHandler.Instance?.DisableItem(this);
        });
    }

    private void OnEnable()
    {
        SteamAvatarManager.Instance.OnTextureLoaded += OnSteamTextureLoaded;
    }

    private void OnDisable()
    {
        if (SteamAvatarManager.Instance != null)
            SteamAvatarManager.Instance.OnTextureLoaded -= OnSteamTextureLoaded;
        Clear();
    }

    private void OnSteamTextureLoaded(ulong steamID, Texture2D tex)
    {
        if (inviterId == steamID)
        {
            InviterAvatar.texture = tex;
        }
    }

    public void OnInviteRecived(ulong lobbyId, ulong userId)
    {
        if (GameManager.GetInstance().IsGameplayScene) return;

        inviterId = userId;
        SetName(userId);

        InviterAvatar.texture = SteamAvatarManager.Instance.GetAvatarTexture(userId);

        AcceptButton.onClick.AddListener(() => AcceptInvite(lobbyId));
    }

    private void AcceptInvite(ulong lobbyId)
    {
        if (SteamLobby.Instance.IsJoining)
        {
            InviteMessageHandler.Instance.DisableItem(this);
            return;
        }

        SteamLobby.Instance.SwitchLobby(lobbyId);

        AcceptButton.onClick.RemoveAllListeners();

        InviterAvatar.texture = null;
        InviterName.text = "";

        InviteMessageHandler.Instance.DisableItem(this);
    }

    private void Clear()
    {
        AcceptButton.onClick.RemoveAllListeners();

        InviterAvatar.texture = null;
        InviterName.text = "";
    }

    private void SetName(ulong id)
    {
        string userName = SteamFriends.GetFriendPersonaName(new CSteamID(id));
        InviterName.text = userName;
    }
}
