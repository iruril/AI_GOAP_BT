using UnityEngine;

public static class AnimHash
{
    public static readonly int XAxis = Animator.StringToHash("XAxis");
    public static readonly int YAxis = Animator.StringToHash("YAxis");
    public static readonly int Accelation = Animator.StringToHash("Accelation");
    public static readonly int AimWeight = Animator.StringToHash("AimWeight");
    public static readonly int CrouchWeight = Animator.StringToHash("CrouchWeight");
    public static readonly int Shootable = Animator.StringToHash("Shootable");
    public static readonly int AngleLerp = Animator.StringToHash("AngleLerp");
    public static readonly int TransitionAccel = Animator.StringToHash("TransitionAccel");

    public static readonly int StartMove_R = Animator.StringToHash("StartMove_R");
    public static readonly int StartMove_L = Animator.StringToHash("StartMove_L");
    public static readonly int Strafe = Animator.StringToHash("Strafe");
    public static readonly int Stop = Animator.StringToHash("Stop");
    public static readonly int Opposite_R = Animator.StringToHash("Opposite_R");
    public static readonly int Opposite_L = Animator.StringToHash("Opposite_L");
    public static readonly int Turn_L = Animator.StringToHash("Turn_L");
    public static readonly int Turn_R = Animator.StringToHash("Turn_R");
    public static readonly int AimTurn_L = Animator.StringToHash("AimTurn_L");
    public static readonly int AimTurn_R = Animator.StringToHash("AimTurn_R");
    public static readonly int CrouchTurn_L = Animator.StringToHash("CrouchTurn_L");
    public static readonly int CrouchTurn_R = Animator.StringToHash("CrouchTurn_R");
    public static readonly int AimCrouchTurn_L = Animator.StringToHash("AimCrouchTurn_L");
    public static readonly int AimCrouchTurn_R = Animator.StringToHash("AimCrouchTurn_R");
    public static readonly int Jump = Animator.StringToHash("Jump");
    public static readonly int Fall = Animator.StringToHash("Fall");
    public static readonly int Land = Animator.StringToHash("Land");

    public static readonly int AimIdle = Animator.StringToHash("AimIdle");
    public static readonly int MagazineReloadNormal = Animator.StringToHash("MagazineReloadNormal");
    public static readonly int MagazineReloadTactical = Animator.StringToHash("MagazineReloadTactical");
    public static readonly int TubeReloadStart = Animator.StringToHash("TubeReloadPump");
    public static readonly int TubeReloadInsert = Animator.StringToHash("TubeReloadInsert");
    public static readonly int TubeReloadEnd = Animator.StringToHash("TubeReloadPump");
}