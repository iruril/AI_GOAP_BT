using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine;
using System.Collections.Generic;

public struct BulletData
{
    public bool IsActive;
    public Vector3 Position;
    public Vector3 Velocity;
    public Vector3 ShotOrigin;

    public float Drag;
    public float Damage;
    public float HeadMultiplier;
    public float RemainingLifeTime;

    public int BulletIndex;
    public LayerMask FriendLayers;
}

[BurstCompile]
public struct BulletMovementJob : IJobParallelFor
{
    public float DeltaTime;
    public NativeArray<BulletData> Bullets;
    [WriteOnly] public NativeArray<RaycastCommand> Commands;
    public LayerMask HitMask;

    public void Execute(int i)
    {
        BulletData bullet = Bullets[i];

        if (!bullet.IsActive)
        {
            var emptyParams = new QueryParameters(0, false, QueryTriggerInteraction.Collide, false);
            Commands[i] = new RaycastCommand(Vector3.zero, Vector3.forward, emptyParams, 0f);
            return;
        }

        bullet.RemainingLifeTime -= DeltaTime;
        if (bullet.RemainingLifeTime <= 0)
        {
            bullet.IsActive = false;
            Bullets[i] = bullet; 
            var emptyParams = new QueryParameters(0, false, QueryTriggerInteraction.Collide, false);
            Commands[i] = new RaycastCommand(Vector3.zero, Vector3.forward, emptyParams, 0f);
            return;
        }

        Vector3 prevPos = bullet.Position;

        bullet.Velocity.y -= 9.81f * DeltaTime;
        bullet.Velocity *= Mathf.Exp(-bullet.Drag * DeltaTime);
        Vector3 nextPos = prevPos + bullet.Velocity * DeltaTime;

        Vector3 dir = nextPos - prevPos;
        float dist = dir.magnitude;

        Vector3 rayDir = dist > 0.00001f ? dir / dist : Vector3.forward;

        QueryParameters queryParams = new QueryParameters(HitMask, false, QueryTriggerInteraction.Collide, false);

        Commands[i] = new RaycastCommand(prevPos, rayDir, queryParams, dist);

        bullet.Position = nextPos;
        Bullets[i] = bullet;
    }
}

public class BulletSimulator : MonoBehaviour
{
    public static BulletSimulator Instance;

    [Header("Settings")]
    [SerializeField] private int maxBullets = 2000;
    [SerializeField] private int maxHitsPerBullet = 4;

    [Header("Masks")]
    [SerializeField] private LayerMask hitMask;   // 벽, 캐릭터 피격 박스
    [SerializeField] private LayerMask grazeMask; // 스침 감지용 박스

    private NativeArray<BulletData> _bulletDatas;
    private NativeArray<RaycastCommand> _commands;
    private NativeArray<RaycastHit> _results;

    private Bullet[] _visuals;
    private Stack<int> _freeIndices;

    private void Awake()
    {
        Instance = this;

        _bulletDatas = new NativeArray<BulletData>(maxBullets, Allocator.Persistent);
        _commands = new NativeArray<RaycastCommand>(maxBullets, Allocator.Persistent);
        _results = new NativeArray<RaycastHit>(maxBullets * maxHitsPerBullet, Allocator.Persistent);

        _visuals = new Bullet[maxBullets];
        _freeIndices = new Stack<int>(maxBullets);

        for (int i = maxBullets - 1; i >= 0; i--)
        {
            _freeIndices.Push(i);
        }
    }

    public int RegisterBullet(Bullet visual, BulletData data)
    {
        if (_freeIndices.Count == 0)
        {
            Debug.LogWarning("Bullet limit reached!");
            return -1;
        }

        int index = _freeIndices.Pop();

        data.BulletIndex = index;
        data.IsActive = true;

        _bulletDatas[index] = data;
        _visuals[index] = visual;

        return index;
    }

    public void UnregisterBullet(int index)
    {
        if (index < 0 || index >= maxBullets) return; 
        if (!_bulletDatas.IsCreated) return;

        BulletData data = _bulletDatas[index];
        if (!data.IsActive) return;

        data.IsActive = false;
        _bulletDatas[index] = data;
        _visuals[index] = null;

        _freeIndices.Push(index);
    }

    private void FixedUpdate()
    {
        var moveJob = new BulletMovementJob
        {
            DeltaTime = Time.fixedDeltaTime,
            Bullets = _bulletDatas,
            Commands = _commands,
            HitMask = hitMask | grazeMask
        };

        JobHandle moveHandle = moveJob.Schedule(maxBullets, 64);

        JobHandle raycastHandle = RaycastCommand.ScheduleBatch(
            _commands,
            _results,
            1,
            maxHitsPerBullet,
            moveHandle
        );

        raycastHandle.Complete();
        ProcessResults();
    }

    private void ProcessResults()
    {
        for (int i = 0; i < maxBullets; i++)
        {
            BulletData data = _bulletDatas[i]; 
            if (!data.IsActive)
            {
                if (_visuals[i] != null)
                {
                    _visuals[i].Deactivate();
                }
                continue;
            }

            if (_visuals[i] == null)
            {
                data.IsActive = false;
                _bulletDatas[i] = data;
                _freeIndices.Push(i);
                continue;
            }

            int resultStartIndex = i * maxHitsPerBullet;

            float closestStopDist = float.MaxValue;
            RaycastHit stopHit = default;
            bool hasStopHit = false;

            for (int h = 0; h < maxHitsPerBullet; h++)
            {
                RaycastHit hit = _results[resultStartIndex + h];
                if (hit.collider == null) continue;

                if (IsInLayerMask(hit.collider.gameObject.layer, hitMask))
                {
                    if (_visuals[i].IsValidHit(hit))
                    {
                        if (hit.distance < closestStopDist)
                        {
                            closestStopDist = hit.distance;
                            stopHit = hit;
                            hasStopHit = true;
                        }
                    }
                }
            }

            bool bulletStopped = false;

            for (int h = 0; h < maxHitsPerBullet; h++)
            {
                RaycastHit hit = _results[resultStartIndex + h];
                if (hit.collider == null) continue;

                if (hasStopHit && hit.distance > closestStopDist + 0.001f) continue;

                if (IsInLayerMask(hit.collider.gameObject.layer, grazeMask))
                {
                    _visuals[i].OnGraze(hit);
                }
            }

            if (hasStopHit)
            {
                _visuals[i].OnHit(stopHit);
                bulletStopped = true;
            }

            if (!bulletStopped)
            {
                _visuals[i].SyncLogicPosition(data.Position);
            }
        }
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (_bulletDatas.IsCreated) _bulletDatas.Dispose();
        if (_commands.IsCreated) _commands.Dispose();
        if (_results.IsCreated) _results.Dispose();
    }

#if UNITY_EDITOR
    private void OnApplicationQuit()
    {
        if (Instance == this) Instance = null;

        if (_bulletDatas.IsCreated) _bulletDatas.Dispose();
        if (_commands.IsCreated) _commands.Dispose();
        if (_results.IsCreated) _results.Dispose();
    }
#endif
}