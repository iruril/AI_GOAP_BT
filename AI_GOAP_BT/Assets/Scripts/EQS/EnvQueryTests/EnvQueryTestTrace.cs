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
                Vector3 itemPosition = item.GetWorldPosition() + Vector3.up * ItemHeightOffset;
                Vector3 direction = (TraceFrom.position + Vector3.up * TargetHeightOffset) - itemPosition;

                if(IsBlocked(itemPosition, direction))
                {
                    item.TestResults[currentTest] = 0.0f;
                }
                else
                {
                    if (traceType == TraceType.Visible)
                    {
                        item.TestResults[currentTest] = 1.0f;
                    }
                    else if (traceType == TraceType.Invisible)
                    {
                        item.TestResults[currentTest] = -1.0f;
                    }
                }
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

    private bool IsBlocked(Vector3 itemPosition, Vector3 direction)
    {
        if (Physics.Linecast(itemPosition, TraceFrom.position, out RaycastHit hitinfo, targetLayers))
        {
            if (hitinfo.transform.root != owner.transform) return true;
            else return false;
        }
        else if(Physics.Linecast(TraceFrom.position, itemPosition, out hitinfo, targetLayers))
        {
            if (hitinfo.transform.root != owner.transform) return true;
            else return false;
        }
        else
        {
            return false; 
        }
    }
}