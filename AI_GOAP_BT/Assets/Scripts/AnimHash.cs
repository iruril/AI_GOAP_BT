using UnityEngine;

public static class AnimHash
{
    public static readonly int XAxis = Animator.StringToHash("XAxis");
    public static readonly int YAxis = Animator.StringToHash("YAxis");
    public static readonly int Accelation = Animator.StringToHash("Accelation");
    public static readonly int AngleLerp = Animator.StringToHash("AngleLerp");
    public static readonly int SpeedOnStop = Animator.StringToHash("SpeedOnStop");

    public static readonly int StartMove_R = Animator.StringToHash("StartMove_R");
    public static readonly int StartMove_L = Animator.StringToHash("StartMove_L");
    public static readonly int Strafe = Animator.StringToHash("Strafe");
}