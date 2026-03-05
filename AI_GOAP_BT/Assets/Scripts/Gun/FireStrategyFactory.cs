using System.Collections.Generic;
using UnityEngine;

public interface IGunFireStrategy
{
    void ExecuteFire(GunHandler handler, Vector3 muzzlePos, Vector3 muzzleDir, float lagTime);
}

public class StandardFireStrategy : IGunFireStrategy
{
    public virtual void ExecuteFire(GunHandler handler, Vector3 muzzlePos, Vector3 muzzleDir, float lagTime)
    {
        handler.ConsumeAmmo(1);
        handler.AddSpread(1f / handler.CurrentGun.GunInfo.Stability);

        float currentSpreadRad = handler.CurrentSpread * Mathf.Deg2Rad;
        SpawnSingleBullet(handler, muzzlePos, muzzleDir, currentSpreadRad, lagTime);

        handler.RpcPlayMuzzleFlash(muzzlePos, Quaternion.LookRotation(muzzleDir));
    }

    protected void SpawnSingleBullet(GunHandler handler, Vector3 muzzlePos, Vector3 muzzleDir, float spreadRad, float lagTime)
    {
        Vector2 error = MathUtility.SampleGaussian2D(spreadRad);
        Vector3 localDir = new Vector3(error.x, error.y, 1f).normalized;

        Quaternion basis = Quaternion.LookRotation(muzzleDir);
        Vector3 finalDir = basis * localDir;

        handler.SpawnAndBroadcastBullet(muzzlePos, finalDir, lagTime);
    }
}

public class ShotgunFireStrategy : StandardFireStrategy
{
    public override void ExecuteFire(GunHandler handler, Vector3 muzzlePos, Vector3 muzzleDir, float lagTime)
    {
        handler.ConsumeAmmo(1);
        handler.AddSpread(1f / handler.CurrentGun.GunInfo.Stability);

        int pelletCount = handler.CurrentGun.GunInfo.PelletCount;
        float maxSpreadRad = handler.CurrentGun.GunInfo.Spread * Mathf.Deg2Rad;

        for (int i = 0; i < pelletCount; i++)
        {
            SpawnSingleBullet(handler, muzzlePos, muzzleDir, maxSpreadRad, lagTime);
        }

        handler.RpcPlayMuzzleFlash(muzzlePos, Quaternion.LookRotation(muzzleDir));
    }
}

public static class FireStrategyFactory
{
    private static readonly StandardFireStrategy standard = new StandardFireStrategy();
    private static readonly ShotgunFireStrategy shotgun = new ShotgunFireStrategy();

    public static IGunFireStrategy GetStrategy(GunType type)
    {
        return type == GunType.Shotgun ? shotgun : standard;
    }
}