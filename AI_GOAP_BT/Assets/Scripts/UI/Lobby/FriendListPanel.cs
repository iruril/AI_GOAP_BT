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

    private void Start()
    {
        CloseButton.onClick.AddListener(DisablePanel);
        SteamAvatarManager.Instance.OnTextureLoaded += OnSteamTextureLoaded;
        DisablePanel();
    }

    private void OnDestroy()
    {
        if (SteamAvatarManager.Instance != null)
            SteamAvatarManager.Instance.OnTextureLoaded -= OnSteamTextureLoaded;
    }

    private void OnEnable()
    {
        ClearPanel();
        GetFriendList();
    }

    private void OnSteamTextureLoaded(ulong steamID, Texture2D tex)
    {
        if (items.TryGetValue(steamID, out var friendItem))
        {
            friendItem.SetAvatar(tex);
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

        Texture2D avatarTexture = SteamAvatarManager.Instance.GetAvatarTexture(steamId.m_SteamID);

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
}
