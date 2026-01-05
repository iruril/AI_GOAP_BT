using Mirror;
using UnityEngine;

public enum Team
{
    Blue,
    Red
}

public class BotSpawner : NetworkBehaviour
{
    public static BotSpawner Instance;

    [Header("Bot Prefab")]
    [SerializeField] GameObject botPrefab;

    private const string BOT = "[BOT]";
    private const string EDEN = "EDEN_";
    private const string REBEL = "REBEL_";

    public bool BotSpawned { get; private set; } = false;

    private void Awake()
    {
        BotSpawned = false;
    }

    public override void OnStartServer()
    {
        Instance = this;
    }

    public override void OnStopServer()
    {
        Instance = null;
    }

    [Server]
    public void SpawnBots()
    {
        SpawnBotsForTeam(Team.Blue);
        SpawnBotsForTeam(Team.Red);
    }

    [Server]
    private void SpawnBotsForTeam(Team team)
    {
        var spawnPoints =
            SpawnPointManager.Instance.GetRemainingSpawnPoints(team);

        int index = 1;

        foreach (var point in spawnPoints)
        {
            string serial = index.ToString("D2");
            string nickname =
                team == Team.Blue
                ? $"{BOT}{EDEN}{serial}"
                : $"{BOT}{REBEL}{serial}";

            GameObject bot = Instantiate(
                botPrefab,
                point.position,
                point.rotation
            );

            if (bot.TryGetComponent<Stat>(out var stat))
            {
                stat.Nickname = nickname;
                stat.SetTeam(team);
            }

            NetworkServer.Spawn(bot);
            index++;
        }

        BotSpawned = true;
    }
}
