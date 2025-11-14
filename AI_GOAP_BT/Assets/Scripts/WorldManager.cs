using System.Collections.Generic;
using UnityEngine;

public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance = null;

    [SerializeField] LayerMask levelLayers;
    [SerializeField] CapturePoint.CapturePoint[] captures;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(this.gameObject);
            return;
        }
    }

    public LayerMask GetLevelLayers()
    {
        return levelLayers;
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
            if (cp.GetCurrentState() == CapturePoint.CaptureState.CapturedByBlue 
                && agent.CompareTag("TeamBlue"))
                continue;

            if (cp.GetCurrentState() == CapturePoint.CaptureState.CapturedByRed
                && agent.CompareTag("TeamRed"))
                continue;

            float dist = Vector3.SqrMagnitude(cp.transform.position - origin);

            if (dist < bestDist)
            {
                bestDist = dist;
                bestPos = cp.transform.position;
                resultCap = cp;
            }
        }

        destination = AstarPath.active.GetNearest(GetRandomPointAround(bestPos, error)).position;
        return resultCap;
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
}
