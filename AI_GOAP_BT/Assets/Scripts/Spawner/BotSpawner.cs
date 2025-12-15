using Mirror;
using UnityEngine;

public class BotSpawner : NetworkBehaviour
{
    [Header("Bot Model")]
    [SerializeField] GameObject teamBlueBot;
    [SerializeField] GameObject teamRedBot;

    [Header("Bot SpawnPoint")]
    [SerializeField] Transform[] teamBlueSpawnPoints = new Transform[12];
    [SerializeField] Transform[] teamRedSpawnPoints = new Transform[12];

    private const string BOT = "[BOT]";
    private const string EDEN = "EDEN_";
    private const string REBEL = "REBEL_";

    public override void OnStartServer()
    {
        SpawnBots(teamBlueBot, teamBlueSpawnPoints, true);
        SpawnBots(teamRedBot, teamRedSpawnPoints, false);
    }

    [Server]
    public void SpawnBots(GameObject botPrefab, Transform[] spawnPoints, bool isTeamBlue)
    {
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            string serialNum = (i + 1).ToString("D2");
            string nickname =
            isTeamBlue
            ? $"{BOT}{EDEN}{serialNum}"
            : $"{BOT}{REBEL}{serialNum}";

            Transform t = spawnPoints[i];
            GameObject bot = Instantiate(botPrefab, t.position, t.rotation); 
            
            if (bot.TryGetComponent<Stat>(out var stat))
            {
                bot.name = nickname;
                stat.Nickname = nickname; // 서버에서 세팅 → 자동 Sync
            }

            NetworkServer.Spawn(bot);
        }
    }
}
