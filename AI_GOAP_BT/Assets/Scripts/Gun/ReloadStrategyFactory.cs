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
        return false; // 탄창식은 캔슬 불가
    }

    public IEnumerator<float> ExecuteReload(GunHandler handler, double startTime)
    {
        handler.OnReload = true;

        bool isTactical = handler.CurrentRounds > 0;

        int animHash = isTactical ? AnimHash.MagazineReloadTactical : AnimHash.MagazineReloadNormal;
        float reloadTime = isTactical ? 1.2f : 1.66f; // TODO: GunInfo에서 받아오기

        handler.PerformReloadAnimation(animHash, 0f, 1f, startTime, 0.25f);
        handler.RpcUpdateReloadAnimation(animHash, 0f, 1f, startTime, 0.25f);

        yield return Timing.WaitForSeconds(reloadTime);

        int capacity = handler.CurrentGun.GunInfo.MagazineCapacity;
        handler.CurrentRounds = isTactical ? capacity + 1 : capacity;

        handler.OnReload = false;
        handler.PerformReloadAnimation(AnimHash.AimIdle, 1f, 0f, NetworkTime.time, 0.25f);
        handler.RpcUpdateReloadAnimation(AnimHash.AimIdle, 1f, 0f, NetworkTime.time, 0.25f);
    }
}

public class TubeReloadStrategy : IGunReloadStrategy
{
    private bool isPumpOpen = false;
    private bool isUncancelable = false;

    public bool TryInterrupt(GunHandler handler, bool isPressed)
    {
        if (!isPressed) return false;
        if (isUncancelable) return false;

        Timing.KillCoroutines(handler.reloadHandle);
        if (isPumpOpen)
        {
            isPumpOpen = false;
            handler.reloadHandle = Timing.RunCoroutine(PumpCloseRoutine(handler));
            return false;
        }
        else
        {
            handler.OnReload = false;
            handler.PerformReloadAnimation(AnimHash.AimIdle, 1f, 0f, NetworkTime.time, 0.1f);
            handler.RpcUpdateReloadAnimation(AnimHash.AimIdle, 1f, 0f, NetworkTime.time, 0.1f);
            return true;
        }
    }

    private IEnumerator<float> PumpCloseRoutine(GunHandler handler)
    {
        isUncancelable = true;

        handler.PerformReloadAnimation(AnimHash.TubeReloadEnd, 0f, 1f, NetworkTime.time, 0.1f);
        handler.RpcUpdateReloadAnimation(AnimHash.TubeReloadEnd, 0f, 1f, NetworkTime.time, 0.1f);

        yield return Timing.WaitForSeconds(0.3f);

        isUncancelable = false;
        handler.OnReload = false;

        handler.PerformReloadAnimation(AnimHash.AimIdle, 1f, 0f, NetworkTime.time, 0.25f);
        handler.RpcUpdateReloadAnimation(AnimHash.AimIdle, 1f, 0f, NetworkTime.time, 0.25f);
    }

    public IEnumerator<float> ExecuteReload(GunHandler handler, double startTime)
    {
        handler.OnReload = true; 
        isPumpOpen = false;
        isUncancelable = false;

        bool isTactical = handler.CurrentRounds > 0;

        if (!isTactical)
        {
            handler.PerformReloadAnimation(AnimHash.TubeReloadStart, 0f, 1f, startTime, 0.25f);
            handler.RpcUpdateReloadAnimation(AnimHash.TubeReloadStart, 0f, 1f, startTime, 0.25f);
            yield return Timing.WaitForSeconds(0.3f);

            isPumpOpen = true;
            isUncancelable = false;
        }

        int capacity = isTactical ? handler.CurrentGun.GunInfo.MagazineCapacity + 1 : handler.CurrentGun.GunInfo.MagazineCapacity;
        while (handler.CurrentRounds < capacity)
        {
            handler.PerformReloadAnimation(AnimHash.TubeReloadInsert, 0f, 1f, NetworkTime.time, 0.1f);
            handler.RpcUpdateReloadAnimation(AnimHash.TubeReloadInsert, 0f, 1f, NetworkTime.time, 0.1f);

            yield return Timing.WaitForSeconds(0.4f);

            handler.CurrentRounds++;
        }

        if (isPumpOpen)
        {
            isUncancelable = true;
            isPumpOpen = false;

            handler.PerformReloadAnimation(AnimHash.TubeReloadEnd, 0f, 1f, NetworkTime.time, 0.25f);
            handler.RpcUpdateReloadAnimation(AnimHash.TubeReloadEnd, 0f, 1f, NetworkTime.time, 0.25f);
            yield return Timing.WaitForSeconds(0.3f);
        }

        isUncancelable = false;
        handler.OnReload = false;

        handler.PerformReloadAnimation(AnimHash.AimIdle, 1f, 0f, NetworkTime.time, 0.25f);
        handler.RpcUpdateReloadAnimation(AnimHash.AimIdle, 1f, 0f, NetworkTime.time, 0.25f);
    }
}

public static class ReloadStrategyFactory
{
    public static IGunReloadStrategy CreateStrategy(ReloadType type)
    {
        return type == ReloadType.Tube ? new TubeReloadStrategy() : new MagazineReloadStrategy();
    }
}