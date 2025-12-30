using Mirror;
using UnityEngine;

public class RoomManager : NetworkRoomManager
{
    private int spawnedGamePlayers = 0;
    private bool botsSpawned = false;

    [Server]
    private void TrySpawnBots()
    {
        if (botsSpawned) return;

        int expectedPlayers = NetworkServer.connections.Count;

        if (spawnedGamePlayers < expectedPlayers)
            return;

        BotSpawner.Instance.SpawnBots();
        botsSpawned = true;
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

        RoomPlayer rp = roomPlayer.GetComponent<RoomPlayer>();
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
