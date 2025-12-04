using UnityEngine;

public static class AnimHash
{
    public static readonly int XAxis = Animator.StringToHash("XAxis");
    public static readonly int YAxis = Animator.StringToHash("YAxis");
    public static readonly int Accelation = Animator.StringToHash("Accelation");
    public static readonly int AimWeight = Animator.StringToHash("AimWeight");
    public static readonly int Shootable = Animator.StringToHash("Shootable");
    public static readonly int AngleLerp = Animator.StringToHash("AngleLerp");
    public static readonly int TransitionAccel = Animator.StringToHash("TransitionAccel");

    public static readonly int StartMove_R = Animator.StringToHash("StartMove_R");
    public static readonly int StartMove_L = Animator.StringToHash("StartMove_L");
    public static readonly int Strafe = Animator.StringToHash("Strafe");
    public static readonly int Stop = Animator.StringToHash("Stop");
    public static readonly int Opposite_R = Animator.StringToHash("Opposite_R");
    public static readonly int Opposite_L = Animator.StringToHash("Opposite_L");
    public static readonly int Reload = Animator.StringToHash("Reload");
    public static readonly int Turn_L = Animator.StringToHash("Turn_L");
    public static readonly int Turn_R = Animator.StringToHash("Turn_R");
    public static readonly int AimTurn_L = Animator.StringToHash("AimTurn_L");
    public static readonly int AimTurn_R = Animator.StringToHash("AimTurn_R");
}