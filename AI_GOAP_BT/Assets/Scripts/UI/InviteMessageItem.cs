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

    protected Callback<AvatarImageLoaded_t> ImageLoaded;

    private void Awake()
    {
        DeclineButton.onClick.AddListener(() =>
        {
            InviteMessageHandler.Instance.DisableItem(this);
        });
    }

    private void OnEnable()
    {
        ImageLoaded = Callback<AvatarImageLoaded_t>.Create(OnImageLoaded);
    }

    private void OnDisable()
    {
        ImageLoaded?.Dispose();
        ImageLoaded = null;

        Clear();
    }

    private void OnImageLoaded(AvatarImageLoaded_t callback)
    {
        if (inviterId != callback.m_steamID.m_SteamID) return;
        InviterAvatar.texture = GetAvatarAsTexture2D(callback.m_iImage);
    }

    public void OnInviteRecived(ulong lobbyId, ulong userId)
    {
        if (GameManager.GetInstance().IsGameplayScene) return;

        inviterId = userId;

        SetName(userId);
        SetAvatar(userId);

        AcceptButton.onClick.AddListener(() => AcceptInvite(lobbyId));
    }

    private void AcceptInvite(ulong lobbyId)
    {
        if (SteamLobby.Instance.IsJoining)
        {
            InviteMessageHandler.Instance.DisableItem(this);
            return;
        }

        SteamLobby.Instance.JoinLobby(lobbyId);

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

    private void SetAvatar(ulong id)
    {
        int avatarInt = SteamFriends.GetLargeFriendAvatar(new CSteamID(id));

        if (avatarInt == -1) return;
        Texture2D avatarTex = GetAvatarAsTexture2D(avatarInt);

        if (avatarTex != null)
        {
            InviterAvatar.texture = avatarTex;
        }
    }

    private Texture2D GetAvatarAsTexture2D(int avatarInt)
    {
        uint width, height;

        bool isVaild = SteamUtils.GetImageSize(avatarInt, out width, out height);

        if (!isVaild || width == 0 || height == 0)
            return null;

        byte[] avatarRaw = new byte[width * height * 4];

        isVaild = SteamUtils.GetImageRGBA(avatarInt, avatarRaw, (int)(width * height * 4));
        if (!isVaild) return null;

        Texture2D avatarTex = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false, true);
        avatarTex.LoadRawTextureData(avatarRaw);
        avatarTex.Apply();

        return avatarTex;
    }
}
