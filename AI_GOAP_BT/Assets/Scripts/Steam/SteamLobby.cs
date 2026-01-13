using UnityEngine;
using Mirror;
using Steamworks;
using System.Collections;

public class SteamLobby : MonoBehaviour
{
    public static SteamLobby Instance;

    protected Callback<LobbyCreated_t> LobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> JoinRequest;
    protected Callback<LobbyEnter_t> LobbyEntered;

    public ulong CurrentLobbyID;
    private const string HostAddressKey = "CustomHostAddress";
    private RoomManager manager;

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

    private IEnumerator InitSteamLobby()
    {
        yield return new WaitUntil(() => SteamManager.Initialized);
        manager = NetworkManager.singleton as RoomManager;

        LobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        JoinRequest = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequest);
        LobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    //버튼 등으로 로비 호스트할 때 실행
    public void HostLobby()
    {
        if (isJoining)
            return;

        CleanupSession();
        SteamMatchmaking.CreateLobby(
            ELobbyType.k_ELobbyTypeFriendsOnly,
            manager.maxConnections
        );
    }

    //로비가 생성되었을 때 콜백
    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
            return;

        CurrentLobbyID = callback.m_ulSteamIDLobby;

        Debug.Log("Steam Lobby Created");

        manager.StartHost();

        SteamMatchmaking.SetLobbyData(
            new CSteamID(CurrentLobbyID),
            HostAddressKey,
            SteamUser.GetSteamID().ToString()
        );

        SteamMatchmaking.SetLobbyData(
            new CSteamID(CurrentLobbyID),
            "name",
            SteamFriends.GetPersonaName() + "'s Lobby"
        );
    }

    //초대 수락 시 콜백
    private void OnJoinRequest(GameLobbyJoinRequested_t callback)
    {
        Debug.Log("Steam Lobby Invite Accepted");

        if (isJoining)
            return;

        isJoining = true;

        CleanupSession();
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    //로비 입장 시 콜백
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

        manager.networkAddress = hostAddress;
        manager.StartClient();

        isJoining = false;
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

    private void OnDestroy()
    {
        CleanupSession();
    }
}