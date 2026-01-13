using Mirror;
using Steamworks;
using UnityEngine;

public static class GameplaySettings
{
    public static bool SpawnBots = true;
    public static bool FriendlyFire = false;
    public static float RespawnDelay = 10f;
}

public class MatchPopulation
{
    public int TargetPerTeam;
    public int BluePlayers;
    public int RedPlayers;
    public int BlueBots;
    public int RedBots;
}

public class RoomManager : NetworkRoomManager
{
    private int spawnedGamePlayers = 0;
    private int expectedGamePlayers = 0;
    private bool botsSpawned = false;
    public bool BotSpawned => botsSpawned;

    public MatchPopulation population = new();

    public override void OnRoomServerPlayersReady()
    {
        base.OnRoomServerPlayersReady();

        int totalPlayers = 16;
        population.TargetPerTeam = Mathf.CeilToInt(totalPlayers / 2f);

        population.BluePlayers = 0;
        population.RedPlayers = 0;
        population.BlueBots = 0;
        population.RedBots = 0;

        if (!Utils.IsSceneActive(RoomScene))
            return;

        expectedGamePlayers = NetworkServer.connections.Count;
        spawnedGamePlayers = 0;
        botsSpawned = false;
    }

    public override void OnRoomServerSceneChanged(string sceneName)
    {
        base.OnRoomServerSceneChanged(sceneName);

        if (SteamLobby.Instance == null)
            return;

        var lobbyId = new CSteamID(SteamLobby.Instance.CurrentLobbyID);

        if (sceneName == RoomScene)
        {
            SteamMatchmaking.SetLobbyData(lobbyId, "state", "lobby");

            spawnedGamePlayers = 0;
            expectedGamePlayers = 0;
            botsSpawned = false;

            population.BluePlayers = 0;
            population.RedPlayers = 0;
            population.BlueBots = 0;
            population.RedBots = 0;
        }
        else
        {
            SteamMatchmaking.SetLobbyData(lobbyId, "state", "ingame");
        }
    }

    protected override void SceneLoadedForPlayer(NetworkConnectionToClient conn, GameObject roomPlayer)
    {
        if (Utils.IsSceneActive(RoomScene))
        {
            PendingPlayer pending;
            pending.conn = conn;
            pending.roomPlayer = roomPlayer;
            pendingPlayers.Add(pending);
            botsSpawned = false;
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
        if (stat.MyTeam == Team.Blue)
            population.BluePlayers++;
        else
            population.RedPlayers++;
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

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if (conn.identity != null)
        {
            Stat stat = conn.identity.GetComponent<Stat>();
            if (stat != null && !Utils.IsSceneActive(RoomScene))
            {
                HandlePlayerLeft(stat.MyTeam);
            }
        }

        base.OnServerDisconnect(conn);
    }

    [Server]
    private void HandlePlayerLeft(Team team)
    {
        if (team == Team.Blue)
        {
            population.BluePlayers--;
            TrySpawnBotForTeam(Team.Blue);
        }
        else
        {
            population.RedPlayers--;
            TrySpawnBotForTeam(Team.Red);
        }
    }

    [Server]
    private void TrySpawnBotForTeam(Team team)
    {
        if (team == Team.Blue)
        {
            int need = population.TargetPerTeam
                     - population.BluePlayers
                     - population.BlueBots;

            if (need > 0)
            {
                BotSpawner.Instance.SpawnBots(Team.Blue, need);
                population.BlueBots += need;
            }
        }
        else
        {
            int need = population.TargetPerTeam
                     - population.RedPlayers
                     - population.RedBots;

            if (need > 0)
            {
                BotSpawner.Instance.SpawnBots(Team.Red, need);
                population.RedBots += need;
            }
        }
    }

    [Server]
    private void TrySpawnBots()
    {
        if (botsSpawned) return;
        if (spawnedGamePlayers < expectedGamePlayers) return;

        TrySpawnBotForTeam(Team.Blue);
        TrySpawnBotForTeam(Team.Red);

        botsSpawned = true;
    }
}
