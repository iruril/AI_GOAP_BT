using Mirror;
using UnityEngine;

public class BotSpawner : NetworkBehaviour
{
    [Header("Bot Model")]
    [SerializeField] GameObject bot;

    [Header("Bot SpawnPoint")]
    [SerializeField] Transform[] teamBlueSpawnPoints = new Transform[12];
    [SerializeField] Transform[] teamRedSpawnPoints = new Transform[12];

    private const string BOT = "[BOT]";
    private const string EDEN = "EDEN_";
    private const string REBEL = "REBEL_";

    public override void OnStartServer()
    {
        SpawnBots(bot, teamBlueSpawnPoints, true);
        SpawnBots(bot, teamRedSpawnPoints, false);
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
            bot.gameObject.layer = isTeamBlue ? LayerMask.NameToLayer("TeamBlue") : LayerMask.NameToLayer("TeamRed");

            if (bot.TryGetComponent<Stat>(out var stat))
            {
                bot.name = nickname;
                stat.Nickname = nickname; // 서버에서 세팅 → 자동 Sync
            }

            NetworkServer.Spawn(bot);
        }
    }
}
