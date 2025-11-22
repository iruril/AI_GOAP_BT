using System.Collections.Generic;
using UnityEngine;
using MEC;

[RequireComponent(typeof(Collider))]
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
    private LayerMask vfxLayers;
    private LayerMask friendLayers;

    private Vector3 velocity;
    private Vector3 prevPos;

    private Vector3 shotOrigin;

    private bool hitProcessed = false;
    private bool initialized = false;

    private void Awake()
    {
        vfxLayers = WorldManager.Instance.GetVFXLayers();
    }

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
        if (!initialized || hitProcessed) return;
        PerformContinuousHitCheck();
    }

    private void PerformContinuousHitCheck()
    {
        float dt = Time.deltaTime;

        velocity.y += gravity * dt;
        velocity *= (1f - drag * dt);

        Vector3 nextPos = transform.position + velocity * dt;
        Vector3 rayDir = nextPos - prevPos;
        float rayDist = rayDir.magnitude;

        if (rayDist > 0.0001f)
        {
            if (Physics.Raycast(prevPos, rayDir.normalized, out var hit, rayDist, ~vfxLayers))
            {
                ProcessHit(hit.collider, hit.point, hit.normal);
                return;
            }
        }

        transform.position = nextPos;
        prevPos = nextPos;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!initialized || hitProcessed) return;

        int otherLayerBit = 1 << other.gameObject.layer;
        if ((vfxLayers & otherLayerBit) != 0) return;
        if ((friendLayers & otherLayerBit) != 0) return;

        ProcessHit(other, other.ClosestPoint(transform.position), -transform.forward);
    }

    private void ProcessHit(Collider target, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (target.TryGetComponent<IDamageable>(out var dmg))
        {
            dmg.ApplyDamage(damage, shotOrigin, hitPoint);
        }

        Quaternion rot = Quaternion.LookRotation(hitNormal);
        EffectPoolManager.SpawnFromPool("Hit", hitPoint, rot);

        Deactivate();
    }

    private void Deactivate()
    {
        hitProcessed = false;
        initialized = false;
        gameObject.SetActive(false);
    }

    public void SetBulletPool(BulletPool pool) => myPool = pool;
}