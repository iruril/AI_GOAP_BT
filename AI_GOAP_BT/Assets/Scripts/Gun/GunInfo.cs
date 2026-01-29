using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public enum FireMode
{
    Single = 0,
    Burst = 1,
    Auto = 2
}

[System.Serializable]
public class GunInfo
{
    public float TimeToADS { get;}
    public int RoundDamage { get; }
    public float Stability { get; }
    public float Spread { get; }
    public float RecoilPitch { get; }
    public float RecoilYawLeft { get; }
    public float RecoilYawRight { get; }
    public float RecoilRoll { get; }
    public int MagazineCapacity { get; }
    public int RPM { get; }
    public float ProjectileSpeed { get; }
    public List<FireMode> FireModes { get; } = new();
    public float ShotInterval { get; }
    public float HeadDamageMultiplier { get; }

    [JsonConstructor]
    public GunInfo(float TimeToADS,
        int RoundDamage,
        float Stability,
        float Spread,
        float RecoilPitch,
        float RecoilYawLeft,
        float RecoilYawRight,
        float RecoilRoll,
        int MagazineCapacity,
        int RPM,
        float ProjectileSpeed,
        List<FireMode> FireModes,
        float headDamageMultiplier)
    {
        this.TimeToADS = TimeToADS;
        this.RoundDamage = RoundDamage;
        this.Stability = Stability;
        this.Spread = Spread;
        this.RecoilPitch = RecoilPitch;
        this.RecoilYawLeft = RecoilYawLeft;
        this.RecoilYawRight = RecoilYawRight;
        this.RecoilRoll = RecoilRoll;
        this.MagazineCapacity = MagazineCapacity;
        this.RPM = RPM;
        this.ProjectileSpeed = ProjectileSpeed;
        if (FireModes.Count > 0)
        {
            this.FireModes = FireModes;
        }

        ShotInterval = 60f / this.RPM;
        HeadDamageMultiplier = headDamageMultiplier;
    }
}

[System.Serializable]
public class Gun
{
    public string GunID { get; }
    public string GunName { get; }
    public GunInfo GunInfo { get; }
    public Vector3 GunPosition { get; }
    public Vector3 MuzzlePosition { get; }
    public Vector3 AimStandardPosition { get; }
    public Vector3 LeftHandIKPosition { get; }
    public Vector3 LeftHandIKRotation { get; }

    [JsonConstructor]
    public Gun(string GunID, string GunName, GunInfo GunInfo,
    float[] GunPosition, float[] MuzzlePosition, float[] AimStandardPosition,
    float[] LeftHandIKPosition, float[] LeftHandIKRotation)
    {
        this.GunID = GunID;
        this.GunName = GunName;
        this.GunInfo = GunInfo;
        this.GunPosition = MathUtility.ArrayToVector3(GunPosition);
        this.MuzzlePosition = MathUtility.ArrayToVector3(MuzzlePosition);
        this.AimStandardPosition = MathUtility.ArrayToVector3(AimStandardPosition);
        this.LeftHandIKPosition = MathUtility.ArrayToVector3(LeftHandIKPosition);
        this.LeftHandIKRotation = MathUtility.ArrayToVector3(LeftHandIKRotation);
    }
}