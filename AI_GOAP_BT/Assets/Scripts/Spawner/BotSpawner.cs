using Mirror;
using UnityEngine;
using System.Collections.Generic;

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
    
    private readonly List<Stat> blueBots = new();
    private readonly List<Stat> redBots = new();

    private int blueBotIndex = 1;
    private int redBotIndex = 1;

    public override void OnStartServer()
    {
        Instance = this;
    }

    public override void OnStopServer()
    {
        Instance = null;
    }

    [Server]
    public void SpawnBots(Team team, int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnSingleBot(team);
        }
    }

    [Server]
    private void SpawnSingleBot(Team team)
    {
        Transform point =
            SpawnPointManager.Instance.ReserveSpawnPoint(team);

        if (point == null)
        {
            Debug.LogWarning("No spawn point for bot");
            return;
        }

        string nickname = GenerateBotName(team);

        GameObject bot = Instantiate(
            botPrefab,
            point.position,
            point.rotation
        );

        if (bot.TryGetComponent(out Stat stat))
        {
            stat.Nickname = nickname;
            stat.SetTeam(team);
        }

        NetworkServer.Spawn(bot);

        if (team == Team.Blue)
            blueBots.Add(stat);
        else
            redBots.Add(stat);
    }

    private string GenerateBotName(Team team)
    {
        if (team == Team.Blue)
            return $"{BOT}{EDEN}{blueBotIndex++:D2}";
        else
            return $"{BOT}{REBEL}{redBotIndex++:D2}";
    }

    [Server]
    public void RemoveOneBot(Team team)
    {
        var list = team == Team.Blue ? blueBots : redBots;
        if (list.Count == 0) return;

        Stat bot = list[0];
        list.RemoveAt(0);

        NetworkServer.Destroy(bot.gameObject);

        RoomManager rm = NetworkManager.singleton as RoomManager;
        if (rm != null)
        {
            if (team == Team.Blue)
                rm.population.BlueBots--;
            else
                rm.population.RedBots--;
        }
    }

    [Server]
    public void ClearAllBots()
    {
        foreach (var bot in blueBots)
            NetworkServer.Destroy(bot.gameObject);

        foreach (var bot in redBots)
            NetworkServer.Destroy(bot.gameObject);

        blueBots.Clear();
        redBots.Clear();

        blueBotIndex = 1;
        redBotIndex = 1;
    }
}
