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
    private LayerMask hitMask;

    private Vector3 velocity;
    private Vector3 prevPos;

    private Vector3 shotOrigin;

    private bool hitProcessed = false;
    private bool initialized = false;

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

    private void Awake()
    {
        hitMask = ~(WorldManager.Instance.GetVFXLayers() | WorldManager.Instance.GetActorLayers());
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
        velocity.y += gravity * Time.deltaTime;
        velocity *= (1f - drag * Time.deltaTime);

        Vector3 nextPos = transform.position + velocity * Time.deltaTime;
        Vector3 rayDir = nextPos - prevPos;
        float rayDist = rayDir.magnitude;

        if (rayDist > 0.0001f)
        {
            if (Physics.Raycast(prevPos, rayDir.normalized, out var hit, rayDist, hitMask))
            {
                ProcessHit(hit.collider, hit.point, hit.normal);
                return;
            }
        }

        transform.position = nextPos;
        prevPos = nextPos;
    }

    private void ProcessHit(Collider target, Vector3 hitPoint, Vector3 hitNormal)
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
        hitProcessed = false;
        initialized = false;
        gameObject.SetActive(false);
    }

    public void SetBulletPool(BulletPool pool) => myPool = pool;
}