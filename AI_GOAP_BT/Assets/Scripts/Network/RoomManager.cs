using Mirror;
using UnityEngine;
using Steamworks;

public static class GameplaySettings
{
    public static bool SpawnBots = true;
    public static bool FriendlyFire = false;
    public static float RespawnDelay = 10f;
}

public class RoomManager : NetworkRoomManager
{
    private int spawnedGamePlayers = 0;

    [Server]
    private void TrySpawnBots()
    {
        int expectedPlayers = NetworkServer.connections.Count;

        if (spawnedGamePlayers < expectedPlayers)
            return;

        BotSpawner.Instance.SpawnBots();
    }

    protected override void SceneLoadedForPlayer(NetworkConnectionToClient conn, GameObject roomPlayer)
    {
        if (Utils.IsSceneActive(RoomScene))
        {
            PendingPlayer pending;
            pending.conn = conn;
            pending.roomPlayer = roomPlayer;
            pendingPlayers.Add(pending);
            return;
        }

        LobbyPlayer rp = roomPlayer.GetComponent<LobbyPlayer>();
        Transform spawnPoint = SpawnPointManager.Instance.ReserveSpawnPoint(rp.MyTeam);

        GameObject gamePlayer = Instantiate(playerPrefab);

        CharacterController cc = gamePlayer.GetComponent<CharacterController>();
        cc.enabled = false;
        gamePlayer.transform.position = spawnPoint ? spawnPoint.position : Vector3.zero;
        gamePlayer.transform.rotation = spawnPoint ? spawnPoint.rotation : Quaternion.identity;
        cc.enabled = true;

        Stat stat = gamePlayer.GetComponent<Stat>();
        stat.SetTeam(rp.MyTeam);
        stat.Nickname = rp.Nickname;

        if (!OnRoomServerSceneLoadedForPlayer(conn, roomPlayer, gamePlayer))
            return;

        NetworkServer.ReplacePlayerForConnection(
            conn,
            gamePlayer,
            ReplacePlayerOptions.KeepAuthority
        );

        spawnedGamePlayers++;
        TrySpawnBots();
    }
}
