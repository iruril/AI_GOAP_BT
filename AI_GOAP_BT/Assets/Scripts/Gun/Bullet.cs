using MEC;
using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class RaycastHitDistanceComparer : IComparer<RaycastHit>
{
    public static readonly RaycastHitDistanceComparer Instance = new RaycastHitDistanceComparer();
    public int Compare(RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance);
}

public class Bullet : MonoBehaviour
{
    private GunHandler owner;
    public void SetOwner(GunHandler gun)
    {
        owner = gun;
        gunName = owner.CurrentGun.GunName;
    }

    private string gunName;

    [SerializeField]
    private float lifeTime = 5f;
    private CoroutineHandle lifeHandle;

    [Header("Ballistics")]
    [SerializeField] private float gravity = 0f;
    [SerializeField] private float drag = 0f;

    private float damage = 1f;
    private float headMultiplier = 1.0f;
    private LayerMask friendLayers;
    [SerializeField] private LayerMask hitMask;
    [SerializeField] private LayerMask grazeMask;

    private Vector3 velocity;

    private Vector3 visualPrevPos;
    private Vector3 logicPos;

    private Vector3 shotOrigin;

    private bool initialized = false; 
    
    private RaycastHit[] hitBuffer = new RaycastHit[4];

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
        BulletPool.ReturnToPool(gameObject);
    }

    public void Init(LayerMask teamLayer, Vector3 shotOrigin, float projectileSpeed, float damage, float headMultiplier, float lagTime)
    {
        friendLayers = teamLayer;

        this.shotOrigin = shotOrigin;
        this.velocity = transform.forward * projectileSpeed;
        this.damage = damage;
        this.headMultiplier = headMultiplier;

        initialized = true;
        if (lagTime > 0) AdvanceProjectile(lagTime);

        logicPos = transform.position;
        visualPrevPos = logicPos;

        lifeHandle = Timing.RunCoroutine(LifeTimer());
    }

    private void AdvanceProjectile(float simulateTime)
    {
        Vector3 startPos = transform.position;

        Vector3 simulatedVelocity = velocity;
        simulatedVelocity.y += gravity * simulateTime;

        simulatedVelocity *= Mathf.Exp(-drag * simulateTime);

        Vector3 predictedPos = startPos + (velocity + simulatedVelocity) * 0.5f * simulateTime;

        Vector3 dir = predictedPos - startPos;
        float dist = dir.magnitude;

        if (dist > 0.00001f)
        {
            int count = Physics.RaycastNonAlloc(startPos, dir.normalized, hitBuffer, dist, hitMask | grazeMask);

            if (count > 0)
            {
                if (ProcessDamageHits(count)) return;
            }
        }

        transform.position = predictedPos;
        velocity = simulatedVelocity;
        logicPos = transform.position;
    }

    private void Update()
    {
        if (!initialized) return;
        float interpolationFactor = (Time.time - Time.fixedTime) / Time.fixedDeltaTime;
        transform.position = Vector3.Lerp(visualPrevPos, logicPos, interpolationFactor);
    }

    private void FixedUpdate()
    {
        if (!initialized) return;

        visualPrevPos = logicPos;

        velocity.y += gravity * Time.fixedDeltaTime;
        velocity *= Mathf.Exp(-drag * Time.fixedDeltaTime);

        Vector3 nextLogicPos = logicPos + velocity * Time.fixedDeltaTime;
        Vector3 dir = nextLogicPos - logicPos;
        float dist = dir.magnitude;

        if (dist > 0.00001f)
        {
            int count = Physics.RaycastNonAlloc(logicPos, dir.normalized, hitBuffer, dist, hitMask | grazeMask);

            if (count > 0)
            {
                if (NetworkServer.active) ProcessGrazingHits(count);
                if (ProcessDamageHits(count)) return;
            }
        }
        logicPos = nextLogicPos;
    }

    private void ProcessGrazingHits(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var hit = hitBuffer[i];
            var col = hit.collider;
            int layer = col.gameObject.layer;

            if ((grazeMask & (1 << layer)) == 0) continue;

            if (col.TryGetComponent<GrazeListener>(out var listener))
            {
                listener.OnGraze(shotOrigin, friendLayers);
            }
        }
    }

    private bool ProcessDamageHits(int count)
    {
        System.Array.Sort(hitBuffer, 0, count, RaycastHitDistanceComparer.Instance);

        for (int i = 0; i < count; i++)
        {
            var hit = hitBuffer[i];
            int layer = hit.collider.gameObject.layer;

            if (IsFriendly(layer) || (hitMask & (1 << layer)) == 0) continue;

            ProcessDamageHit(hit.collider, hit.point, hit.normal);
            return true;
        }

        return false;
    }

    private void ProcessDamageHit(Collider target, Vector3 hitPoint, Vector3 hitNormal)
    {
        bool isServer = NetworkServer.active && owner != null;

        if (target.TryGetComponent<HitBox>(out var hitBox))
        {
            // isServer가 false면 데미지 로직만 내부적으로 스킵됨
            hitBox.ApplyDamage(
                damage,
                headMultiplier,
                shotOrigin,
                hitPoint,
                friendLayers,
                isServer ? owner.gameObject.transform : null,
                WorldManager.Instance.IsBlueTeam(friendLayers),
                gunName
            );
        }

        if (isServer)
        {
            string vfxName = ((1 << target.gameObject.layer) & WorldManager.Instance.GetBleedLayers()) != 0 ? "Blood" : "Hit";
            owner.ServerReportHit(hitPoint, Quaternion.LookRotation(hitNormal), vfxName);
        }

        Deactivate();
    }

    private bool IsFriendly(int layer) => (friendLayers.value & (1 << layer)) != 0;
    private void Deactivate() { initialized = false; gameObject.SetActive(false); }
    IEnumerator<float> LifeTimer() { yield return Timing.WaitForSeconds(lifeTime); gameObject.SetActive(false); }
}