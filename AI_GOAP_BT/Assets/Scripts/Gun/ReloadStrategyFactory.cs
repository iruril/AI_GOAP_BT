using System.Collections.Generic;
using MEC;
using Mirror;
using UnityEngine;

public interface IGunReloadStrategy
{
    bool TryInterrupt(GunHandler handler, bool isPressed);
    IEnumerator<float> ExecuteReload(GunHandler handler, double startTime);
}

public class MagazineReloadStrategy : IGunReloadStrategy
{
    public bool TryInterrupt(GunHandler handler, bool isPressed)
    {
        // 탄창식은 중간에 절대 캔슬할 수 없음!
        return false;
    }

    public IEnumerator<float> ExecuteReload(GunHandler handler, double startTime)
    {
        handler.OnReload = true;
        handler.PerformReloadAnimation(AnimHash.MagazineReload, 0f, 1f, startTime, 0.25f);
        handler.RpcUpdateReloadAnimation(AnimHash.MagazineReload, 0f, 1f, startTime, 0.25f);

        // TODO: 향후 애니메이션 길이를 GunInfo에서 받아오기.
        yield return Timing.WaitForSeconds(1.66f);

        int capacity = handler.CurrentGun.GunInfo.MagazineCapacity;
        handler.CurrentRounds = (handler.CurrentRounds == 0) ? capacity : capacity + 1;

        handler.OnReload = false;
        handler.PerformReloadAnimation(AnimHash.AimIdle, 1f, 0f, NetworkTime.time, 0.25f);
        handler.RpcUpdateReloadAnimation(AnimHash.AimIdle, 1f, 0f, NetworkTime.time, 0.25f);
    }
}

public class TubeReloadStrategy : IGunReloadStrategy
{
    public bool TryInterrupt(GunHandler handler, bool isPressed)
    {
        if (isPressed)
        {
            handler.OnReload = false;
            Timing.KillCoroutines(handler.reloadHandle);

            handler.PerformReloadAnimation(AnimHash.AimIdle, 1f, 0f, NetworkTime.time, 0.1f);
            handler.RpcUpdateReloadAnimation(AnimHash.AimIdle, 1f, 0f, NetworkTime.time, 0.1f);
            return true; // 캔슬 성공!
        }
        return false;
    }

    public IEnumerator<float> ExecuteReload(GunHandler handler, double startTime)
    {
        handler.OnReload = true;
        handler.PerformReloadAnimation(AnimHash.TubeReloadStart, 0f, 1f, startTime, 0.25f);
        handler.RpcUpdateReloadAnimation(AnimHash.TubeReloadStart, 0f, 1f, startTime, 0.25f);

        yield return Timing.WaitForSeconds(0.3f);

        int capacity = handler.CurrentGun.GunInfo.MagazineCapacity;

        while (handler.CurrentRounds < capacity)
        {
            handler.PerformReloadAnimation(AnimHash.TubeReloadInsert, 0f, 1f, NetworkTime.time, 0.1f);
            handler.RpcUpdateReloadAnimation(AnimHash.TubeReloadInsert, 0f, 1f, NetworkTime.time, 0.1f);

            yield return Timing.WaitForSeconds(0.4f);

            handler.CurrentRounds++;
        }

        handler.PerformReloadAnimation(AnimHash.TubeReloadEnd, 0f, 1f, NetworkTime.time, 0.25f);
        handler.RpcUpdateReloadAnimation(AnimHash.TubeReloadEnd, 0f, 1f, NetworkTime.time, 0.25f);
        yield return Timing.WaitForSeconds(0.3f);

        handler.OnReload = false;

        handler.PerformReloadAnimation(AnimHash.AimIdle, 1f, 0f, NetworkTime.time, 0.25f);
        handler.RpcUpdateReloadAnimation(AnimHash.AimIdle, 1f, 0f, NetworkTime.time, 0.25f);
    }
}

public static class ReloadStrategyFactory
{
    private static readonly MagazineReloadStrategy magazine = new MagazineReloadStrategy();
    private static readonly TubeReloadStrategy tube = new TubeReloadStrategy();

    public static IGunReloadStrategy GetStrategy(ReloadType type)
    {
        return type == ReloadType.Tube ? tube : magazine;
    }
}