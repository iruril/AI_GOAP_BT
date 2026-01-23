using System;
using UnityEngine;
using Mirror;
using Steamworks;
using System.Collections;

public class SteamLobby : MonoBehaviour
{
    public static SteamLobby Instance;

    public event Action<ulong, ulong> OnInviteRecieced;
    public event Action<bool> OnJoiningStateChanged;
    public event Action<LobbyCreateOptions> OnLobbyOptionChanged;

    public enum LobbyVisibility
    {
        Public = 0,
        FriendsOnly = 1,
        Private = 2
    }

    [System.Serializable]
    public struct LobbyCreateOptions
    {
        public LobbyVisibility lobbyVisibility;
        public bool UsePassword;
        public string Password;
        public int MaxPlayers;
        public bool SpawnBots;
        public bool FriendlyFire;
        public float RespawnDelay;
    }

    private LobbyCreateOptions currentLobbyOptions;
    public LobbyCreateOptions CurrentLobbyOptions 
    {
        get => currentLobbyOptions;
        private set
        {
            if (currentLobbyOptions.Equals(value)) return;

            currentLobbyOptions = value;
            OnLobbyOptionChanged?.Invoke(value);
        }
    }

    private LobbyCreateOptions DefaultOptions => new LobbyCreateOptions
    {
        lobbyVisibility = LobbyVisibility.Public,
        UsePassword = false,
        Password = string.Empty,
        MaxPlayers = 16,
        SpawnBots = true,
        FriendlyFire = false,
        RespawnDelay = 5f
    }; 
    
    private enum LobbyListPurpose
    {
        None,
        Browse,
        RandomJoin
    }

    private LobbyListPurpose currentPurpose = LobbyListPurpose.None;

    protected Callback<LobbyCreated_t> LobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> JoinRequest;
    protected Callback<LobbyEnter_t> LobbyEntered; 
    protected Callback<LobbyMatchList_t> LobbyMatchList;
    protected Callback<LobbyInvite_t> InviteRecieved;

    public ulong CurrentLobbyID;
    private const string HostAddressKey = "CustomHostAddress";

    private RoomManager Manager => NetworkManager.singleton as RoomManager;

    private bool isJoining = false;
    public bool IsJoining 
    { 
        get => isJoining;
        private set
        {
            if (isJoining == value) return;

            isJoining = value;
            OnJoiningStateChanged?.Invoke(value);
        }
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        StartCoroutine(InitSteamLobby());
    }

    private void OnDestroy()
    {
        LobbyCreated?.Dispose();
        JoinRequest?.Dispose();
        LobbyEntered?.Dispose();
        LobbyMatchList?.Dispose();
        InviteRecieved?.Dispose();

        LobbyCreated = null;
        JoinRequest = null;
        LobbyEntered = null;
        LobbyMatchList = null;
        InviteRecieved = null;

        CleanupSession();
    }

    private IEnumerator InitSteamLobby()
    {
        yield return new WaitUntil(() => SteamManager.Initialized);

        LobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        JoinRequest = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequest);
        LobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        LobbyMatchList = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);
        InviteRecieved = Callback<LobbyInvite_t>.Create(OnLobbyInvite);
    }

    #region Steam Callbacks
    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
            return;

        CurrentLobbyID = callback.m_ulSteamIDLobby;
        var lobbyId = new CSteamID(CurrentLobbyID);

        Debug.Log("Steam Lobby Created");

        SteamMatchmaking.SetLobbyData(
            lobbyId,
            HostAddressKey,
            SteamUser.GetSteamID().ToString()
        );

        // 표시용 이름
        SteamMatchmaking.SetLobbyData(
            lobbyId,
            "name",
            SteamFriends.GetPersonaName() + "'s Lobby"
        );

        string visibilityStr = currentLobbyOptions.lobbyVisibility switch
        {
            LobbyVisibility.Public => "public",
            LobbyVisibility.FriendsOnly => "friends",
            _ => "private"
        };

        SteamMatchmaking.SetLobbyData(lobbyId, "visibility", visibilityStr);
        SteamMatchmaking.SetLobbyData(lobbyId, "state", "lobby");
        string hasPassword = currentLobbyOptions.UsePassword ? "true" : "false";
        SteamMatchmaking.SetLobbyData(lobbyId, "hasPassword", hasPassword);
        SteamMatchmaking.SetLobbyData(lobbyId, "Password", currentLobbyOptions.Password);
        SteamMatchmaking.SetLobbyData(lobbyId, "version", Application.version);
        SteamMatchmaking.SetLobbyData(lobbyId, "maxPlayers", currentLobbyOptions.MaxPlayers.ToString());
        SteamMatchmaking.SetLobbyData(lobbyId, "spawnBots", currentLobbyOptions.SpawnBots ? "true" : "false");
        SteamMatchmaking.SetLobbyData(lobbyId, "friendlyFire", currentLobbyOptions.FriendlyFire ? "true" : "false");
        SteamMatchmaking.SetLobbyData(lobbyId, "respawnDelay", currentLobbyOptions.RespawnDelay.ToString());

        var manager = Manager;
        if (manager == null)
        {
            Debug.LogWarning("RoomManager not ready");
            return;
        }
        Manager.StartHost();
    }

    private void OnJoinRequest(GameLobbyJoinRequested_t callback)
    {
        Debug.Log("Steam Lobby Invite Accepted");

        if (IsJoining)
            return;

        IsJoining = true;
        CleanupSession(false);

        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        CurrentLobbyID = callback.m_ulSteamIDLobby;

        Debug.Log("Entered Steam Lobby");

        if (NetworkServer.active)
        {
            IsJoining = false;
            return;
        }

        string hostAddress = SteamMatchmaking.GetLobbyData(
            new CSteamID(CurrentLobbyID),
            HostAddressKey
        );

        var manager = Manager;
        if (manager == null)
        {
            Debug.LogWarning("RoomManager not ready");
            return;
        }

        manager.networkAddress = hostAddress; 
        manager.StartClient();

        IsJoining = false;
    }

    private void OnLobbyMatchList(LobbyMatchList_t callback)
    {
        switch (currentPurpose)
        {
            case LobbyListPurpose.Browse:
                HandleBrowseLobbyList(callback);
                break;

            case LobbyListPurpose.RandomJoin:
                HandleRandomJoin(callback);
                break;
        }

        currentPurpose = LobbyListPurpose.None;
    }

    private void OnLobbyInvite(LobbyInvite_t callback)
    {
        OnInviteRecieced?.Invoke(callback.m_ulSteamIDLobby, callback.m_ulSteamIDUser);
    }
    #endregion

    public void HostLobby()
    {
        HostLobby(DefaultOptions);
    }

    public void HostLobby(LobbyCreateOptions options)
    {
        if (IsJoining)
            return;

        CleanupSession();

        CurrentLobbyOptions = options;
        ELobbyType lobbyType;

        switch (options.lobbyVisibility)
        {
            case LobbyVisibility.Public:
                lobbyType = ELobbyType.k_ELobbyTypePublic;
                break;
            case LobbyVisibility.FriendsOnly:
                lobbyType = ELobbyType.k_ELobbyTypeFriendsOnly;
                break;
            default:
                lobbyType = ELobbyType.k_ELobbyTypePrivate;
                break;
        }

        SteamMatchmaking.CreateLobby(
            lobbyType,
            options.MaxPlayers
        );
    }

    public void LeaveLobby()
    {
        IsJoining = false;

        if (CurrentLobbyID != 0)
        {
            SteamMatchmaking.LeaveLobby(new CSteamID(CurrentLobbyID));
            CurrentLobbyID = 0;
        }

        if (NetworkServer.active || NetworkClient.active)
        {
            NetworkManager.singleton.StopHost();
        }
    }

    public void JoinLobby(ulong lobbyID)
    {
        if (IsJoining)
            return;

        IsJoining = true;
        CleanupSession(false);

        SteamMatchmaking.JoinLobby(new CSteamID(lobbyID));
    }

    public void JoinRandomLobby()
    {
        if (IsJoining)
            return;

        IsJoining = true;
        CleanupSession(false);

        currentPurpose = LobbyListPurpose.RandomJoin;

        SteamMatchmaking.AddRequestLobbyListStringFilter(
            "state",
            "lobby",
            ELobbyComparison.k_ELobbyComparisonEqual
        ); 
        SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
        SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(1);
        SteamMatchmaking.RequestLobbyList();
    }

    public void RequestLobbyList()
    {
        currentPurpose = LobbyListPurpose.Browse;

        SteamMatchmaking.AddRequestLobbyListResultCountFilter(50);

        SteamMatchmaking.AddRequestLobbyListStringFilter(
            "state",
            "lobby",
            ELobbyComparison.k_ELobbyComparisonEqual
        );

        SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(1);

        SteamMatchmaking.RequestLobbyList();
    }

    private void HandleRandomJoin(LobbyMatchList_t cb)
    {
        if (cb.m_nLobbiesMatching == 0)
        {
            Debug.Log("랜덤 입장 가능한 로비가 없습니다.");
            IsJoining = false;
            currentPurpose = LobbyListPurpose.None;
            return;
        }

        int index = UnityEngine.Random.Range(0, (int)cb.m_nLobbiesMatching);
        CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(index);

        Debug.Log($"[RandomJoin] {lobbyId.m_SteamID}");
        SteamMatchmaking.JoinLobby(lobbyId);
    }

    private void HandleBrowseLobbyList(LobbyMatchList_t cb)
    {
        if (LobbyBrowser.Instance == null)
            return;

        LobbyBrowser.Instance.ClearLobbies();

        for (int i = 0; i < cb.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
            LobbyBrowser.Instance.AddLobby(lobbyId.m_SteamID);
        }
    }

    private void CleanupSession(bool resetJoinFlag = true)
    {
        if (resetJoinFlag)
            IsJoining = false;

        // 기존 로비 탈퇴
        if (CurrentLobbyID != 0)
        {
            SteamMatchmaking.LeaveLobby(new CSteamID(CurrentLobbyID));
            CurrentLobbyID = 0;
        }

        // 네트워크 정리
        if (NetworkServer.active)
        {
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.active)
        {
            NetworkManager.singleton.StopClient();
        }
    }
}