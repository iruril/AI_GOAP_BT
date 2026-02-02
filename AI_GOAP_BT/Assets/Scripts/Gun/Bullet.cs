using Mirror;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private GunHandler owner;
    private string gunName;
    private int _simulationIndex = -1; // 시뮬레이터에서의 인덱스

    [SerializeField] private float lifeTime = 5f;

    [Header("Ballistics")]
    [SerializeField] private float drag = 0.1f;

    private float damage = 1f;
    private float headMultiplier = 1.0f;
    private LayerMask friendLayers;
    private Vector3 shotOrigin;

    // 보간을 위한 변수
    private Vector3 visualPrevPos;
    private Vector3 logicPos;
    private bool initialized = false;

    public void SetOwner(GunHandler gun)
    {
        owner = gun;
        gunName = owner.CurrentGun.GunName;
    }

    private void OnEnable()
    {
        initialized = false;
        _simulationIndex = -1;
    }

    private void OnDisable()
    {
        if (_simulationIndex != -1 && BulletSimulator.Instance != null)
        {
            BulletSimulator.Instance.UnregisterBullet(_simulationIndex);
            _simulationIndex = -1;
        }

        initialized = false;
        owner = null;
        BulletPool.ReturnToPool(gameObject);
    }

    public void Init(LayerMask teamLayer, Vector3 origin, float projectileSpeed, float damage, float headMultiplier, float lagTime)
    {
        friendLayers = teamLayer;
        shotOrigin = origin;
        this.damage = damage;
        this.headMultiplier = headMultiplier;

        logicPos = transform.position;
        visualPrevPos = logicPos;

        BulletData data = new BulletData
        {
            IsActive = true,
            Position = transform.position,
            Velocity = transform.forward * projectileSpeed,
            ShotOrigin = origin,
            Drag = drag,
            Damage = damage,
            HeadMultiplier = headMultiplier,
            RemainingLifeTime = lifeTime,
            FriendLayers = teamLayer,
            BulletIndex = -1
        };

        // Lag Compensation (lagTime만큼 미리 전진)
        if (lagTime > 0)
        {
            data.Position += data.Velocity * lagTime;
            logicPos = data.Position;
            visualPrevPos = logicPos;
            transform.position = logicPos;
        }

        _simulationIndex = BulletSimulator.Instance.RegisterBullet(this, data);

        if (_simulationIndex != -1)
        {
            initialized = true;
        }
        else
        {
            Deactivate();
        }
    }

    public void Deactivate()
    {
        initialized = false;
        gameObject.SetActive(false);
    }

    public void SyncLogicPosition(Vector3 newPos)
    {
        visualPrevPos = logicPos;
        logicPos = newPos;
    }

    private void Update()
    {
        if (!initialized) return;

        float interpolationFactor = (Time.time - Time.fixedTime) / Time.fixedDeltaTime;
        transform.position = Vector3.Lerp(visualPrevPos, logicPos, interpolationFactor);

        Vector3 dir = logicPos - visualPrevPos;
        if (dir.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    public bool IsValidHit(RaycastHit hit)
    {
        int layer = hit.collider.gameObject.layer;

        if (IsFriendly(layer)) return false;

        return true;
    }

    public void OnGraze(RaycastHit hit)
    {
        if (IsFriendly(hit.collider.gameObject.layer)) return;

        if (hit.collider.TryGetComponent<GrazeListener>(out var listener))
        {
            listener.OnGraze(shotOrigin, friendLayers);
        }
    }

    public void OnHit(RaycastHit hit)
    {
        ProcessDamageHit(hit.collider, hit.point, hit.normal);
        Deactivate();
    }

    private void ProcessDamageHit(Collider target, Vector3 hitPoint, Vector3 hitNormal)
    {
        bool isServer = NetworkServer.active && owner != null;

        if (target.TryGetComponent<HitBox>(out var hitBox))
        {
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
    }

    private bool IsFriendly(int layer) => (friendLayers.value & (1 << layer)) != 0;
}