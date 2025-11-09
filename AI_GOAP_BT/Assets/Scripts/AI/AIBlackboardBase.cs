using UnityEngine;

public class AIBlackboardBase : MonoBehaviour
{
    [Header("Target / Combat")]
    public bool EngageAvailable = false;
    public bool Reloading = false;

    [Header("Cover & Retreat")]
    public bool InCover = false;
    public bool InRetreat = false;
    public bool HasPeakAngle = false;

    [Header("Capture")]
    public bool HasCapturePoint = false;
    public bool Capturing = false;
}
