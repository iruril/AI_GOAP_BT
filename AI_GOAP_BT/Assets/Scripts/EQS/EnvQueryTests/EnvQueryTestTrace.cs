using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnvQueryTestTrace : EnvQueryTest
{
    private enum TraceType
    {
        Visible,
        Invisible
    }

    [SerializeField]
    private Transform owner;

    [SerializeField]
    private TraceType traceType;
    [SerializeField]
    private LayerMask targetLayers;

    public Transform TraceFrom; // TraceFrom

    public float ItemHeightOffset;
    public float TargetHeightOffset;

    private static readonly RaycastHit[] hitBuffer = new RaycastHit[1];

    public EnvQueryTestTrace()
    {
        traceType = TraceType.Visible;
    }

    public override void RunTest(int currentTest, List<EnvQueryItem> envQueryItems)
    {
        if (TraceFrom != null && envQueryItems != null)
        {
            foreach(EnvQueryItem item in envQueryItems)
            {
                Vector3 from = item.GetWorldPosition() + Vector3.up * ItemHeightOffset;
                Vector3 to = TraceFrom.position + Vector3.up * TargetHeightOffset;
                Vector3 dir = to - from;
                float dist = dir.magnitude;

                bool blocked = IsBlocked(from, dir.normalized, dist);

                item.TestResults[currentTest] =
                    (traceType == TraceType.Visible)
                    ? (blocked ? 0f : 1f)
                    : (blocked ? 1f : 0f);
            }
        }
        else
        {
            foreach(EnvQueryItem item in envQueryItems)
            {
                item.TestResults[currentTest] = 0.0f;
            }
        }
    }

    private bool IsBlocked(Vector3 origin, Vector3 direction, float distance)
    {
        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            0.25f,
            direction,
            hitBuffer,
            distance,
            targetLayers,
            QueryTriggerInteraction.Ignore
        );

        return hitCount > 0;
    }
}