using UnityEngine;

public interface IFireModeStrategy
{
    FireMode Mode { get; }

    bool CheckCanFire(bool isPressed, bool isHeld, bool isCooldownReady, GunInfo gunInfo);

    void ResetState();
}

public class SingleFireModeStrategy : IFireModeStrategy
{
    public FireMode Mode => FireMode.Single;

    public bool CheckCanFire(bool isPressed, bool isHeld, bool isCooldownReady, GunInfo gunInfo)
    {
        return isCooldownReady && isPressed;
    }

    public void ResetState() { }
}

public class AutoFireModeStrategy : IFireModeStrategy
{
    public FireMode Mode => FireMode.Auto;

    public bool CheckCanFire(bool isPressed, bool isHeld, bool isCooldownReady, GunInfo gunInfo)
    {
        return isCooldownReady && isHeld;
    }

    public void ResetState() { }
}

public class BurstFireModeStrategy : IFireModeStrategy
{
    public FireMode Mode => FireMode.Burst;

    private int currentBurstCount = 0;

    public bool CheckCanFire(bool isPressed, bool isHeld, bool isCooldownReady, GunInfo gunInfo)
    {
        if (isPressed)
        {
            currentBurstCount = 0;
        }

        if (!isCooldownReady) return false;

        if (isHeld && currentBurstCount < gunInfo.BurstCount)
        {
            currentBurstCount++;
            return true;
        }

        return false;
    }

    public void ResetState()
    {
        currentBurstCount = 0;
    }
}

public static class FireModeStrategyFactory
{
    private static readonly SingleFireModeStrategy single = new SingleFireModeStrategy();
    private static readonly AutoFireModeStrategy auto = new AutoFireModeStrategy();

    public static IFireModeStrategy CreateStrategy(FireMode mode)
    {
        switch (mode)
        {
            case FireMode.Single: return single;
            case FireMode.Auto: return auto;
            case FireMode.Burst: return new BurstFireModeStrategy(); // 카운트 독립을 위해 새로 생성
            default: return single;
        }
    }
}