using Pathfinding;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance = null;

    [Header("Layer Masks")]
    [SerializeField] LayerMask levelLayers;
    [SerializeField] LayerMask vfxLayers;
    [SerializeField] LayerMask actorLayers;
    [SerializeField] LayerMask bleedLayers;
    [SerializeField] LayerMask blueTeamLayers;
    [SerializeField] LayerMask redTeamLayers;

    [Header("Capture Points")]
    [SerializeField] CapturePoint.CapturePoint[] captures;

    [Header("Team Colors")]
    [SerializeField] Color blueTeamColor;
    [SerializeField] Color redTeamColor;
    [SerializeField] Color defColor;

    public Color BlueTeamColor { get => blueTeamColor;}
    public Color RedTeamColor { get => redTeamColor;}
    public Color DefColor { get => defColor;}

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this.gameObject);
            return;
        }
        SceneManager.activeSceneChanged += (oldScene, newScene) => OnSceneLoaded(newScene.name);
    }
    
    /// <summary>
    /// 씬 로드 완료 시 CapturePoint 자동 재수집
    /// </summary>
    private void OnSceneLoaded(string sceneName)
    {
        if (!sceneName.Contains("Gameplay")) return;
        RefreshCapturePoints();
    }

    private void RefreshCapturePoints()
    {
        captures = FindObjectsByType<CapturePoint.CapturePoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);

#if UNITY_EDITOR
        Debug.Log($"[WorldManager] CapturePoints refreshed: {captures.Length}");
#endif
    }

    public LayerMask GetLevelLayers()
    {
        return levelLayers;
    }

    public LayerMask GetVFXLayers()
    {
        return vfxLayers;
    }

    public LayerMask GetActorLayers()
    {
        return actorLayers;
    }

    public LayerMask GetBleedLayers()
    {
        return bleedLayers;
    }

    public LayerMask GetShootableLayers()
    {
        return levelLayers | bleedLayers;
    }

    public LayerMask GetRedTeamLayers()
    {
        return redTeamLayers;
    }

    public LayerMask GetBlueTeamLayers()
    {
        return blueTeamLayers;
    }

    public bool IsBlueTeam(LayerMask layerMask)
    {
        return (layerMask & blueTeamLayers) != 0;
    }

    public bool IsRedTeam(LayerMask layerMask)
    {
        return (layerMask & redTeamLayers) != 0;
    }

    public bool IsBlueTeam(int layer)
    {
        return (blueTeamLayers.value & (1 << layer)) != 0;
    }

    public bool IsRedTeam(int layer)
    {
        return (redTeamLayers.value & (1 << layer)) != 0;
    }

    public CapturePoint.CapturePoint[] GetCaptures()
    {
        return captures;
    }

    public bool IsThereUncapturedPoint(Transform agent)
    {
        foreach (var cap in captures)
        {
            if (cap.NeedToCapture(agent))
                return true;
        }
        return false;
    }

    public CapturePoint.CapturePoint RequestClosestCapture(Transform agent, float error, out Vector3 destination)
    {
        CapturePoint.CapturePoint resultCap = null;
        if (captures == null || captures.Length == 0)
        {
            destination = Vector3.negativeInfinity;
            return resultCap;
        }

        float bestDist = float.MaxValue;
        Vector3 bestPos = agent.position;
        Vector3 origin = agent.position;

        foreach (var cp in captures)
        {
            if (!cp.NeedToCapture(agent))
                continue;

            float dist = CalculatePathDistance(origin, cp.transform.position);

            if (dist < bestDist)
            {
                bestDist = dist;
                bestPos = cp.transform.position;
                resultCap = cp;
            }
        }

        destination = FindReachableRandomPoint(bestPos, agent.position, error);
        return resultCap;
    }

    private Vector3 FindReachableRandomPoint(Vector3 center, Vector3 agentPos, float error)
    {
        const int MaxTries = 5;

        var startNode = AstarPath.active.GetNearest(agentPos).node;
        if (startNode == null || !startNode.Walkable)
            return center;

        for (int i = 0; i < MaxTries; i++)
        {
            Vector3 randomPoint = GetRandomPointAround(center, error);

            var endNode = AstarPath.active.GetNearest(randomPoint).node;
            if (endNode == null || !endNode.Walkable)
                continue;

            if (PathUtilities.IsPathPossible(startNode, endNode))
            {
                return (Vector3)endNode.position;
            }
        }

        return center;
    }

    private float CalculatePathDistance(Vector3 start, Vector3 end)
    {
        var path = ABPath.Construct(start, end);
        AstarPath.StartPath(path);
        path.BlockUntilCalculated();

        float total = 0f;
        var pts = path.vectorPath;

        for (int i = 1; i < pts.Count; i++)
            total += Vector3.Distance(pts[i - 1], pts[i]);

        return total;
    }

    private Vector3 GetRandomPointAround(Vector3 center, float radius)
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);

        float r = Mathf.Sqrt(Random.Range(0f, 1f)) * radius;

        float x = Mathf.Cos(angle) * r;
        float z = Mathf.Sin(angle) * r;

        Vector3 point = new Vector3(center.x + x, center.y, center.z + z);

        return point;
    }

    /// <summary>
    /// 모든 점령지의 점령 상태를 합산하여 반환
    /// </summary>
    /// <returns> 블루팀이 우세라면 양수값, 레드팀이 우세라면 음수값, 동등하다면 0이다. </returns>
    public int GetTotalCaptureScore()
    {
        if (captures == null || captures.Length == 0)
            return 0;

        int total = 0;

        foreach (var cap in captures)
        {
            switch (cap.SyncedState)
            {
                case CapturePoint.CaptureState.CapturedByBlue:
                    total += 1;
                    break;

                case CapturePoint.CaptureState.CapturedByRed:
                    total -= 1;
                    break;

                case CapturePoint.CaptureState.Neutral:
                default:
                    // 0이므로 아무것도 안 함
                    break;
            }
        }

        return total;
    }
}
