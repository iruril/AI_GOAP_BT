using UnityEngine;
using Steamworks;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class InviteMessageHandler : MonoBehaviour
{
    public static InviteMessageHandler Instance;

    [SerializeField] private float popUpDuration = 10f;

    public Button AcceptButton;
    public Button DeclineButton;
    public TextMeshProUGUI InviterName;
    public RawImage InviterAvatar;

    ulong currentInviterId;

    protected Callback<AvatarImageLoaded_t> ImageLoaded;

    private void Awake()
    {
        Instance = this;

        StartCoroutine(Init());
    }

    private void OnDestroy()
    {
        ImageLoaded?.Dispose();

        ImageLoaded = null;

        DeclineInvite();

        Instance = null;
    }

    private void Start()
    {
        SteamLobby.Instance.OnInviteRecieced += OnInviteRecived;
        DeclineButton.onClick.AddListener(DeclineInvite);

        gameObject.SetActive(false);
    }

    private IEnumerator Init()
    {
        yield return new WaitUntil(() => SteamManager.Initialized);

        ImageLoaded = Callback<AvatarImageLoaded_t>.Create(OnImageLoaded);
    }

    private void OnImageLoaded(AvatarImageLoaded_t callback)
    {
        if (currentInviterId != callback.m_steamID.m_SteamID) return;
        InviterAvatar.texture = GetAvatarAsTexture2D(callback.m_iImage);
    }

    private void OnInviteRecived(ulong lobbyId, ulong userId)
    {
        currentInviterId = userId;

        SetName(userId);
        SetAvatar(userId);

        AcceptButton.onClick.AddListener(() => AcceptInvite(lobbyId));

        gameObject.SetActive(true);
        StartCoroutine(AutoClose());
    }

    private void AcceptInvite(ulong lobbyId)
    {
        SteamLobby.Instance.JoinLobby(lobbyId);

        AcceptButton.onClick.RemoveAllListeners();

        InviterAvatar.texture = null;
        InviterName.text = "";

        gameObject.SetActive(false);
    }

    private void DeclineInvite()
    {
        StopAllCoroutines();

        AcceptButton.onClick.RemoveAllListeners();

        InviterAvatar.texture = null;
        InviterName.text = "";

        gameObject.SetActive(false);
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

    private IEnumerator AutoClose()
    {
        yield return new WaitForSeconds(popUpDuration);
        DeclineInvite();
    }
}
