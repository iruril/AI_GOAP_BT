using System;
using UnityEngine;
using Mirror;
using Steamworks;
using System.Collections;

public class SteamLobby : MonoBehaviour
{
    public static SteamLobby Instance;

    public event Action<ulong, ulong> OnInviteReceived;
    public event Action<bool> OnJoiningStateChanged;
    public event Action<LobbyCreateOptions> OnLobbyOptionChanged; 
    public event Action<JoinResult> OnJoinResult;

    public enum JoinResult
    {
        Success,
        Failed,
        Timeout,
        Canceled
    }

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
    
    private enum JoinPurpose
    {
        None,
        Join,
        RandomJoin,
        Host
    }

    private JoinPurpose joinPurpose = JoinPurpose.None;

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

    private uint joinRequestToken = 0;
    private uint activeJoinToken = 0;

    private Coroutine joinTimeoutCoroutine;
    private const float JoinTimeoutSeconds = 30f;

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
        
        if (joinTimeoutCoroutine != null)
            StopCoroutine(joinTimeoutCoroutine);
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
        if (joinPurpose != JoinPurpose.Host)
            return;

        if (callback.m_eResult != EResult.k_EResultOK)
        {
            OnJoinResult?.Invoke(JoinResult.Failed);
            CancelJoining();
            return;
        }

        if (joinTimeoutCoroutine != null)
        {
            StopCoroutine(joinTimeoutCoroutine);
            joinTimeoutCoroutine = null;
        }

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

        OnJoinResult?.Invoke(JoinResult.Success);
        IsJoining = false;
        joinPurpose = JoinPurpose.None;
    }

    private void OnJoinRequest(GameLobbyJoinRequested_t callback)
    {
        Debug.Log("Steam Lobby Invite Accepted");

        if (IsJoining)
            return;

        SwitchLobby(callback.m_steamIDLobby.m_SteamID);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        if (!IsJoining)
            return;

        if (activeJoinToken != joinRequestToken)
            return;

        if (joinTimeoutCoroutine != null)
        {
            StopCoroutine(joinTimeoutCoroutine);
            joinTimeoutCoroutine = null;
        }

        CurrentLobbyID = callback.m_ulSteamIDLobby;

        Debug.Log("Entered Steam Lobby");

        if (NetworkServer.active || joinPurpose == JoinPurpose.Host)
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

        OnJoinResult?.Invoke(JoinResult.Success);
        IsJoining = false;
    }

    private void OnLobbyMatchList(LobbyMatchList_t callback)
    {
        switch (joinPurpose)
        {
            case JoinPurpose.Join:
                HandleBrowseLobbyList(callback);
                break;

            case JoinPurpose.RandomJoin:
                HandleRandomJoin(callback);
                break;
        }

        joinPurpose = JoinPurpose.None;
    }

    private void OnLobbyInvite(LobbyInvite_t callback)
    {
        OnInviteReceived?.Invoke(callback.m_ulSteamIDLobby, callback.m_ulSteamIDUser);
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

        joinPurpose = JoinPurpose.Host;
        CurrentLobbyOptions = options;

        BeginJoining();

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

        CleanupSession(false);

        joinPurpose = JoinPurpose.Join;
        BeginJoining();

        SteamMatchmaking.JoinLobby(new CSteamID(lobbyID));
    }

    public void JoinRandomLobby()
    {
        if (IsJoining)
            return;

        CleanupSession(false);

        joinPurpose = JoinPurpose.RandomJoin;
        BeginJoining();

        SteamMatchmaking.AddRequestLobbyListStringFilter(
            "state",
            "lobby",
            ELobbyComparison.k_ELobbyComparisonEqual
        );

        SteamMatchmaking.AddRequestLobbyListStringFilter(
            "hasPassword",
            "true",
            ELobbyComparison.k_ELobbyComparisonNotEqual
        );

        SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
        SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(1);
        SteamMatchmaking.RequestLobbyList();
    }

    public void SwitchLobby(ulong lobbyId)
    {
        if (IsJoining)
            return;

        StartCoroutine(SwitchLobbyRoutine(lobbyId));
    }

    private IEnumerator SwitchLobbyRoutine(ulong lobbyId)
    {
        CleanupSession(false);

        yield return null;
        yield return new WaitForEndOfFrame();

        joinPurpose = JoinPurpose.Join;
        BeginJoining();

        SteamMatchmaking.JoinLobby(new CSteamID(lobbyId));
    }

    public void RequestLobbyList()
    {
        if (IsJoining)
            return;

        joinPurpose = JoinPurpose.Join;

        SteamMatchmaking.AddRequestLobbyListResultCountFilter(50);

        SteamMatchmaking.AddRequestLobbyListStringFilter(
            "state",
            "lobby",
            ELobbyComparison.k_ELobbyComparisonEqual
        );

        SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(1);

        SteamMatchmaking.RequestLobbyList();
    }

    public void CancelJoining(JoinResult result = JoinResult.Canceled)
    {
        if (!IsJoining)
            return;

        activeJoinToken = 0;
        IsJoining = false;

        if (joinTimeoutCoroutine != null)
        {
            StopCoroutine(joinTimeoutCoroutine);
            joinTimeoutCoroutine = null;
        }

        OnJoinResult?.Invoke(result);
        CleanupSession();

        joinPurpose = JoinPurpose.None;
    }

    private void HandleRandomJoin(LobbyMatchList_t cb)
    {
        if (cb.m_nLobbiesMatching == 0)
        {
            Debug.Log("랜덤 입장 가능한 로비가 없습니다.");
            IsJoining = false;
            OnJoinResult?.Invoke(JoinResult.Failed);
            joinPurpose = JoinPurpose.None;
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
        activeJoinToken = 0;

        if (resetJoinFlag)
            IsJoining = false;

        if (CurrentLobbyID != 0)
        {
            SteamMatchmaking.LeaveLobby(new CSteamID(CurrentLobbyID));
            CurrentLobbyID = 0;
        }

        if (NetworkServer.active)
        {
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.active)
        {
            NetworkManager.singleton.StopClient();
        }
    }

    private void BeginJoining()
    {
        activeJoinToken = ++joinRequestToken;

        IsJoining = true;

        if (joinTimeoutCoroutine != null)
            StopCoroutine(joinTimeoutCoroutine);

        joinTimeoutCoroutine = StartCoroutine(JoinTimeoutRoutine(activeJoinToken));
    }

    private IEnumerator JoinTimeoutRoutine(uint token)
    {
        float elapsed = 0f;

        while (elapsed < JoinTimeoutSeconds)
        {
            if (!IsJoining || activeJoinToken != token)
                yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (activeJoinToken == token)
        {
            Debug.LogWarning("Steam Lobby Join Timeout");
            CancelJoining(JoinResult.Timeout);
        }
    }
}