using System.Collections.Generic;
using UnityEngine;
using MEC;
using Mirror;

public class Bullet : MonoBehaviour
{
    private BulletPool myPool;
    private GunHandler owner;
    public void SetOwner(GunHandler gun)
    {
        owner = gun;
    }

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
    private Vector3 prevPos; //FixedUpdate 기준 이전 위치

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
        owner = null;
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

        velocity.y += gravity * Time.deltaTime;
        velocity *= Mathf.Exp(-drag * Time.deltaTime);

        transform.position += velocity * Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (!initialized) return; 

        Vector3 currentPos = transform.position;

        Vector3 dir = currentPos - prevPos;
        float dist = dir.magnitude;

        if (dist > 0.00001f)
        {
            int count = Physics.RaycastNonAlloc(
                prevPos,
                dir.normalized,
                hitBuffer,
                dist,
                hitMask | grazeMask
            );

            if (count > 0)
            {
                if (NetworkServer.active) 
                    ProcessGrazingHits(count);
                if (ProcessDamageHits(count))
                {
                    prevPos = currentPos;
                    return;
                }
            }
        }

        prevPos = currentPos;
    }

    private void ProcessGrazingHits(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var hit = hitBuffer[i];
            var col = hit.collider;
            int layer = col.gameObject.layer;

            if ((grazeMask & (1 << layer)) == 0) continue;
            if (IsFriendly(layer)) continue;

            if (col.TryGetComponent<GrazeListener>(out var listener))
                listener.OnGraze(shotOrigin);
        }
    }

    private bool ProcessDamageHits(int count)
    {
        float closestDist = float.MaxValue;
        RaycastHit closestHit = default;
        bool found = false;

        for (int i = 0; i < count; i++)
        {
            var hit = hitBuffer[i];
            float dist = hit.distance;
            var col = hit.collider;
            int layer = col.gameObject.layer;

            if (IsFriendly(layer)) continue;

            if ((hitMask & (1 << layer)) == 0) continue;

            if(dist < closestDist)
            {
                closestDist = dist;
                closestHit = hit;
                found = true;
            }
        }

        if (!found) return false;

        ProcessDamageHit(closestHit.collider, closestHit.point, closestHit.normal);
        return true;
    }

    private void ProcessDamageHit(Collider target, Vector3 hitPoint, Vector3 hitNormal)
    {
        Quaternion rot = Quaternion.LookRotation(hitNormal);
        string vfxName = "Hit";

        if (target.TryGetComponent<HitBox>(out var hitBox))
        {
            bool result = hitBox.ApplyDamage(damage, shotOrigin, hitPoint, friendLayers, owner != null);
            if (NetworkServer.active && owner != null && result)
            {
                hitBox.SendShooterInfo(
                    owner.gameObject.transform,
                    WorldManager.Instance.IsBlueTeam(friendLayers)
                );
            }
        }

        if (((1 << target.gameObject.layer) & WorldManager.Instance.GetBleedLayers()) != 0)
            vfxName = "Blood";
        else 
            vfxName = "Hit";

        if (NetworkServer.active && owner != null) 
            owner.ServerReportHit(hitPoint, rot, vfxName);
        Deactivate();
    }

    private bool IsFriendly(int layer) => ((1 << layer) & friendLayers) != 0;

    private void Deactivate()
    {
        initialized = false;
        gameObject.SetActive(false);
    }

    public void SetBulletPool(BulletPool pool) => myPool = pool;
}