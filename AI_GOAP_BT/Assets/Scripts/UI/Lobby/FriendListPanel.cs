using UnityEngine;
using UnityEngine.UI;
using Steamworks;
using System.Collections.Generic;
using System;

public class FriendListPanel : MonoBehaviour
{
    [Header("Contents")]
    [SerializeField] GameObject panelContentsPrefab;

    [Header("Content Rect")]
    public RectTransform ContentRect;
    public Button CloseButton;

    private Dictionary<ulong, FriendListItem> items = new();

    protected Callback<AvatarImageLoaded_t> ImageLoaded;

    private void Start()
    {
        ImageLoaded = Callback<AvatarImageLoaded_t>.Create(OnImageLoaded);
        CloseButton.onClick.AddListener(DisablePanel);
        DisablePanel();
    }

    private void OnDestroy()
    {
        ImageLoaded?.Dispose();
        ImageLoaded = null;
    }

    private void OnEnable()
    {
        ClearPanel();
        GetFriendList();
    }

    private void OnImageLoaded(AvatarImageLoaded_t callback)
    {
        if (items.TryGetValue(callback.m_steamID.m_SteamID, out var friend))
        {
            Texture2D texture2D = GetAvatarAsTexture2D(callback.m_iImage);
            friend.SetAvatar(texture2D);
        }
    }

    public void EnablePanel()
    {
        gameObject.SetActive(true);
    }

    public void DisablePanel()
    {
        gameObject.SetActive(false);
    }

    public void AddFriend(CSteamID steamId)
    {
        if (items.ContainsKey(steamId.m_SteamID)) return;

        GameObject go = Instantiate(panelContentsPrefab, ContentRect);
        FriendListItem item = go.GetComponent<FriendListItem>();

        string friendName = SteamFriends.GetFriendPersonaName(steamId);
        int avatarInt = SteamFriends.GetLargeFriendAvatar(steamId);
        Texture2D avatarTexture = GetAvatarAsTexture2D(avatarInt);

        item.SetName(friendName);
        item.SetAvatar(avatarTexture);
        item.InviteButton.onClick.AddListener(() =>
        {
            SteamMatchmaking.InviteUserToLobby((CSteamID)SteamLobby.Instance.CurrentLobbyID, steamId);
        });

        items.Add(steamId.m_SteamID, item);
    }

    private void GetFriendList()
    {
        int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);

        List<CSteamID> friendList = new List<CSteamID>();

        for (int i = 0; i < friendCount; i++)
        {
            CSteamID friendSteamId = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);

            EPersonaState state = SteamFriends.GetFriendPersonaState(friendSteamId);
            if (state == EPersonaState.k_EPersonaStateOffline)
                continue;

            friendList.Add(friendSteamId);
        }

        friendList.Sort((a, b) =>
        {
            int priorityA = GetPriority(SteamFriends.GetFriendPersonaState(a));
            int priorityB = GetPriority(SteamFriends.GetFriendPersonaState(b));

            if (priorityA != priorityB)
                return priorityA.CompareTo(priorityB);

            string nameA = SteamFriends.GetFriendPersonaName(a);
            string nameB = SteamFriends.GetFriendPersonaName(b);
            return string.Compare(nameA, nameB, StringComparison.OrdinalIgnoreCase);
        });

        foreach (var friendSteamId in friendList)
        {
            AddFriend(friendSteamId);
        }
    }

    private int GetPriority(EPersonaState state)
    {
        switch (state)
        {
            case EPersonaState.k_EPersonaStateOnline:
                return 0;
            case EPersonaState.k_EPersonaStateAway:
                return 1;
            case EPersonaState.k_EPersonaStateBusy:
                return 2;
            case EPersonaState.k_EPersonaStateSnooze:
                return 3;
            default:
                return 4;
        }
    }

    private void ClearPanel()
    {
        foreach (var item in items.Values)
        {
            if (item != null) Destroy(item.gameObject);
        }
        items.Clear();
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
