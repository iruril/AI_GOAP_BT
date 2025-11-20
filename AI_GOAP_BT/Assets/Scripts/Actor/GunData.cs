using System.Collections.Generic;
using UnityEngine;

namespace Gun
{
    public enum FireMode
    {
        Single = 0,
        Burst = 1,
        Auto = 2
    }

    [System.Serializable]
    public class GunData
    {
        public float AimSpeed { get; }
        public int RoundDamage { get; }
        public float WeaponStablility { get; }
        public float WeaponSpread { get; }
        public int MagazineCapacity { get; }
        public int RoundsPerMinute { get; }
        public List<FireMode> FireModes = new List<FireMode> { FireMode.Single };

        private float _shotInterval;
        public float ShotInterval { get { return _shotInterval; } }

        public GunData(float AimSpeed,
            int RoundDamage,
            float WeaponStablility,
            float WeaponSpread,
            int MagazineCapacity,
            int RoundsPerMinute,
            List<FireMode> FireModes)
        {
            this.AimSpeed = AimSpeed;
            this.RoundDamage = RoundDamage;
            this.WeaponStablility = WeaponStablility;
            this.WeaponSpread = WeaponSpread;
            this.MagazineCapacity = MagazineCapacity;
            this.RoundsPerMinute = RoundsPerMinute;
            if (FireModes.Count > 0)
            {
                this.FireModes = FireModes;
            }

            _shotInterval = 1 / (float)(this.RoundsPerMinute / 60);
        }
    }

    [System.Serializable]
    public class Gun
    {
        public string WeaponID;
        public string WeaponName;
        public GunData WeaponStat;

        public Vector3 WeaponPosition;
        public Vector3 GunEndPosition;
        public Vector3 LeftHandIKTargetPosition;
        public Vector3 LeftHandIKTargetRotation;

        public Gun(string WeaponID, string WeaponName, GunData WeaponStat,
        float[] WeaponPosition,
        float[] GunEndPosition,
        float[] LeftHandIKTargetPosition,
        float[] LeftHandIKTargetRotation)
        {
            this.WeaponID = WeaponID;
            this.WeaponName = WeaponName;
            this.WeaponStat = WeaponStat;

            this.WeaponPosition = MathUtility.ArrayToVector3(WeaponPosition);
            this.GunEndPosition = MathUtility.ArrayToVector3(GunEndPosition);
            this.LeftHandIKTargetPosition = MathUtility.ArrayToVector3(LeftHandIKTargetPosition);
            this.LeftHandIKTargetRotation = MathUtility.ArrayToVector3(LeftHandIKTargetRotation);
        }
    }
}