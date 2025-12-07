using System.Collections.Generic;
using UnityEngine;
using MEC;

public class Bullet : MonoBehaviour
{
    private BulletPool myPool;

    [SerializeField]
    private float lifeTime = 5f;
    private CoroutineHandle lifeHandle;

    [Header("Ballistics")]
    [SerializeField] private float gravity = 0f;
    [SerializeField] private float drag = 0f;

    private float damage = 1f;
    private LayerMask friendLayers;
    [SerializeField] private LayerMask hitMask;
    [SerializeField] private LayerMask grazeMask;

    private Vector3 velocity;
    private Vector3 prevPos;

    private Vector3 shotOrigin;

    private bool initialized = false; 
    
    private RaycastHit[] hitBuffer = new RaycastHit[8];

    private void OnEnable()
    {
        initialized = false;
        Timing.KillCoroutines(lifeHandle);
    }

    private void OnDisable()
    {
        initialized = false;
        Timing.KillCoroutines(lifeHandle);
        myPool?.ReturnToPool(gameObject);
    }

    public void Init(LayerMask teamLayer, Vector3 shotOrigin, float projectileSpeed, float damage)
    {
        friendLayers = teamLayer;

        this.shotOrigin = shotOrigin;
        this.velocity = transform.forward * projectileSpeed;
        this.damage = damage;

        initialized = true;
        prevPos = transform.position;

        lifeHandle = Timing.RunCoroutine(LifeTimer());
    }

    IEnumerator<float> LifeTimer()
    {
        yield return Timing.WaitForSeconds(lifeTime);
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!initialized) return;
        PerformContinuousHitCheck();
    }

    private void PerformContinuousHitCheck()
    {
        Vector3 nextPos;
        float rayDist;
        Vector3 rayDir;

        ApplyBallistics(out nextPos, out rayDir, out rayDist);

        if (rayDist > 0.0001f)
        {
            int count = DoRaycast(prevPos, rayDir, rayDist);

            if (count > 0)
            {
                ProcessGrazingHits(count);
                if (ProcessDamageHits(count))
                    return;
            }
        }

        transform.position = nextPos;
        prevPos = nextPos;
    }

    private void ApplyBallistics(out Vector3 nextPos, out Vector3 rayDir, out float rayDist)
    {
        velocity.y += gravity * Time.deltaTime;
        velocity *= (1f - drag * Time.deltaTime);

        nextPos = transform.position + velocity * Time.deltaTime;
        rayDir = nextPos - prevPos;
        rayDist = rayDir.magnitude;
    }

    private int DoRaycast(Vector3 origin, Vector3 direction, float distance)
    {
        int combinedMask = hitMask | grazeMask;
        return Physics.RaycastNonAlloc(origin, direction.normalized, hitBuffer, distance, combinedMask);
    }

    private void ProcessGrazingHits(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var hit = hitBuffer[i];
            var col = hit.collider;
            int layer = col.gameObject.layer;

            if ((grazeMask & (1 << layer)) != 0)
            {
                if (col.TryGetComponent<GrazeListener>(out var listener))
                {
                    if (IsFriendly(listener.Owner.gameObject)) continue;
                    listener.OnGraze(shotOrigin);
                }
            }
        }
    }

    private bool ProcessDamageHits(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var hit = hitBuffer[i];
            var col = hit.collider;
            int layer = col.gameObject.layer;

            if (IsFriendly(layer)) continue;
            if ((hitMask & (1 << layer)) != 0)
            {
                ProcessDamageHit(col, hit.point, hit.normal);
                return true;
            }
        }
        return false;
    }

    private bool IsFriendly(int layer)
    {
        return ((1 << layer) & friendLayers) != 0;
    }

    private bool IsFriendly(GameObject obj) => ((1 << obj.layer) & friendLayers) != 0;

    private void ProcessDamageHit(Collider target, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (target.TryGetComponent<HitBox>(out var hitBox))
        {
            hitBox.ApplyDamage(damage, shotOrigin, hitPoint, friendLayers);
        }

        Quaternion rot = Quaternion.LookRotation(hitNormal);
        EffectPoolManager.SpawnFromPool("Hit", hitPoint, rot);

        Deactivate();
    }

    private void Deactivate()
    {
        initialized = false;
        gameObject.SetActive(false);
    }

    public void SetBulletPool(BulletPool pool) => myPool = pool;
}