using Mirror;
using UnityEngine;
using System.Collections.Generic;

public class SpawnPointManager : NetworkBehaviour
{
    public static SpawnPointManager Instance;

    [Header("Team Spawn Points")]
    [SerializeField] private Transform[] blueSpawnPoints;
    [SerializeField] private Transform[] redSpawnPoints;

    private readonly List<Transform> freeBluePoints = new();
    private readonly List<Transform> freeRedPoints = new();

    public override void OnStartServer()
    {
        Instance = this;

        freeBluePoints.Clear();
        freeRedPoints.Clear();

        freeBluePoints.AddRange(blueSpawnPoints);
        freeRedPoints.AddRange(redSpawnPoints);
    }

    [Server]
    public Transform ReserveSpawnPoint(Team team)
    {
        List<Transform> list = team == Team.Blue ? freeBluePoints : freeRedPoints;
        if (list.Count == 0) return null;

        int index = Random.Range(0, list.Count);
        Transform point = list[index];
        list.RemoveAt(index);
        return point;
    }

    [Server]
    public IReadOnlyList<Transform> GetRemainingSpawnPoints(Team team)
    {
        return team == Team.Blue ? freeBluePoints : freeRedPoints;
    }
}
