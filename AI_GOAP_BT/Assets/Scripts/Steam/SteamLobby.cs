using UnityEngine;
using Mirror;
using Steamworks;
using System.Collections;

public class SteamLobby : MonoBehaviour
{
    public static SteamLobby Instance;

    public enum LobbyVisibility
    {
        Public,
        FriendsOnly,
        Private
    }

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

    private LobbyCreateOptions currentOptions;
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

    protected Callback<LobbyCreated_t> LobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> JoinRequest;
    protected Callback<LobbyEnter_t> LobbyEntered; 
    protected Callback<LobbyMatchList_t> LobbyMatchList;

    public ulong CurrentLobbyID;
    private const string HostAddressKey = "CustomHostAddress";
    private RoomManager Manager => NetworkManager.singleton as RoomManager;


    private bool isJoining = false;

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

        LobbyCreated = null;
        JoinRequest = null;
        LobbyEntered = null;
        LobbyMatchList = null;

        CleanupSession();
    }

    private IEnumerator InitSteamLobby()
    {
        yield return new WaitUntil(() => SteamManager.Initialized);

        LobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        JoinRequest = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequest);
        LobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        LobbyMatchList = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);
    }

    public void HostLobby()
    {
        HostLobby(DefaultOptions);
    }

    public void HostLobby(LobbyCreateOptions options)
    {
        if (isJoining)
            return;

        CleanupSession();

        currentOptions = options;
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
        isJoining = false;

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

    //로비가 생성되었을 때 콜백
    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
            return;

        CurrentLobbyID = callback.m_ulSteamIDLobby;
        var lobbyId = new CSteamID(CurrentLobbyID);

        Debug.Log("Steam Lobby Created");

        var manager = Manager;
        if (manager == null)
        {
            Debug.LogWarning("RoomManager not ready");
            return;
        }
        manager.StartHost();

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

        SteamMatchmaking.SetLobbyData(lobbyId, "visibility", "public");
        SteamMatchmaking.SetLobbyData(lobbyId, "state", "lobby");
        SteamMatchmaking.SetLobbyData(lobbyId, "hasPassword", "false");
        SteamMatchmaking.SetLobbyData(lobbyId, "version", Application.version);
        SteamMatchmaking.SetLobbyData(lobbyId, "maxPlayers", currentOptions.MaxPlayers.ToString());
        SteamMatchmaking.SetLobbyData(lobbyId, "spawnbots", currentOptions.SpawnBots ? "true" : "false");
        SteamMatchmaking.SetLobbyData(lobbyId, "friendlyfire", currentOptions.FriendlyFire ? "true" : "false");
        SteamMatchmaking.SetLobbyData(lobbyId, "respawndelay", currentOptions.RespawnDelay.ToString());
    }

    private void OnJoinRequest(GameLobbyJoinRequested_t callback)
    {
        Debug.Log("Steam Lobby Invite Accepted");

        if (isJoining)
            return;

        isJoining = true;

        CleanupSession();
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        CurrentLobbyID = callback.m_ulSteamIDLobby;

        Debug.Log("Entered Steam Lobby");

        if (NetworkServer.active)
        {
            isJoining = false;
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

        isJoining = false;
    }

    public void JoinRandomPublicLobby()
    {
        CleanupSession();

        SteamMatchmaking.AddRequestLobbyListResultCountFilter(20);
        SteamMatchmaking.AddRequestLobbyListStringFilter(
            "visibility",
            "public",
            ELobbyComparison.k_ELobbyComparisonEqual
        );

        SteamMatchmaking.AddRequestLobbyListStringFilter(
            "state",
            "lobby",
            ELobbyComparison.k_ELobbyComparisonEqual
        );

        SteamMatchmaking.RequestLobbyList();
    }

    private void OnLobbyMatchList(LobbyMatchList_t cb)
    {
        if (cb.m_nLobbiesMatching == 0)
        {
            Debug.Log("입장 가능한 공개 로비가 없습니다.");
            return;
        }

        // 랜덤 선택
        int index = Random.Range(0, (int)cb.m_nLobbiesMatching);
        var lobbyId = SteamMatchmaking.GetLobbyByIndex(index);

        Debug.Log($"랜덤 로비 입장: {lobbyId.m_SteamID}");

        SteamMatchmaking.JoinLobby(lobbyId);
    }

    private void CleanupSession()
    {
        // 네트워크 정리
        if (NetworkServer.active || NetworkClient.active)
        {
            NetworkManager.singleton.StopHost();
        }

        // 기존 로비 탈퇴
        if (CurrentLobbyID != 0)
        {
            SteamMatchmaking.LeaveLobby(new CSteamID(CurrentLobbyID));
            CurrentLobbyID = 0;
        }
    }
}