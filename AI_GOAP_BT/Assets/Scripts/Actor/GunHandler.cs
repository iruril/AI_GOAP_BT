using RootMotion.FinalIK;
using UnityEngine;
using System.Collections.Generic;

public class GunHandler : MonoBehaviour
{
    private GOAP.Assualt.AssaultBrain myBrain;
    private BipedIK myIK;
    private AimIK aimIK;

    [Header("Gun 트랜스폼 세팅")]
    [SerializeField] Transform GunPos;
    [SerializeField] Transform LeftHandIKTarget;
    [SerializeField] Transform Muzzle;

    [Header("Aim IK Target 세팅")]
    [SerializeField] Transform AimIKTarget;
    Transform aimTarget;

    private Gun currentGun;
    public Gun CurrentGun { get { return currentGun; } }
    private GameObject currentGunModel;

    private BulletPool bulletPool;

    private Dictionary<string, (Gun gun, GameObject instance)> gunHistory = new(); 
    
    private bool pendingFire = false;

    private float currentSpread = 0;

    void Awake()
    {
        myBrain = GetComponent<GOAP.Assualt.AssaultBrain>();

        myIK = GetComponent<BipedIK>();
        myIK.solvers.leftHand.target = LeftHandIKTarget;
        myIK.solvers.leftHand.IKPositionWeight = 1f;

        aimIK = GetComponent<AimIK>();
        aimIK.solver.IKPositionWeight = 0f;

        bulletPool = GetComponent<BulletPool>();
    }

    void Start()
    {
        LoadGun("AK-15");

        myBrain.Sensor.OnTargetSet += SetTarget;
        myBrain.Sensor.OnTargetReset += ResetTarget;
        myBrain.Sensor.MyStat.OnDead += OnDead;
        myBrain.Sensor.MyStat.OnRevive += OnRevive;
        aimIK.solver.OnPostUpdate += FireCallback;
    }

    private void OnDestroy()
    {
        myBrain.Sensor.OnTargetSet -= SetTarget;
        myBrain.Sensor.OnTargetReset -= ResetTarget;
        myBrain.Sensor.MyStat.OnDead -= OnDead;
        myBrain.Sensor.MyStat.OnRevive -= OnRevive;
        aimIK.solver.OnPostUpdate -= FireCallback;
    }

    void Update()
    {
        AimIKHandle();
        SpreadHandle();
    }

    void LoadGun(string gunName)
    {
        bool cached = false;
        (Gun gun, GameObject instance) gunData;

        if (gunHistory.ContainsKey(gunName))
        {
            cached = true;
            gunData = gunHistory[gunName];
        }
        else
        {
            gunData = GameManager.Instance.GunTable[gunName];
            gunHistory.Add(gunName, gunData);
        }

        currentGun = gunData.gun;

        if (currentGunModel != null)
            currentGunModel.SetActive(false);

        if (!cached)
        {
            currentGunModel = Instantiate(gunData.instance);
        }
        else
        {
            currentGunModel = gunData.instance;
            currentGunModel.SetActive(true);
        }

        currentGunModel.transform.SetParent(GunPos, false);
        currentGunModel.transform.localPosition = Vector3.zero;
        currentGunModel.transform.localRotation = Quaternion.identity;
        currentSpread = 0;

        ApplyGunTransforms(currentGun);
    }

    void ApplyGunTransforms(Gun gunData)
    {
        GunPos.localPosition = gunData.GunPosition;

        Muzzle.localPosition = gunData.MuzzlePosition;

        LeftHandIKTarget.localPosition = gunData.LeftHandIKPosition;
        LeftHandIKTarget.localEulerAngles = gunData.LeftHandIKRotation;
    }

    private void AimIKHandle()
    {
        bool hasTarget = aimTarget != null;
        IKTargetTransformControl(hasTarget);
        IKWeightControl(hasTarget);
    }

    private void IKTargetTransformControl(bool hasTarget)
    {
        AimIKTarget.position = hasTarget ?
                    aimTarget.position + Vector3.up * 1.2f
                    : transform.position + transform.forward * 3.0f + Vector3.up * 1.2f;
    }

    float _refTargetValue;
    private void IKWeightControl(bool hasTarget)
    {
        float _targetVaule = hasTarget && myBrain.MotionController.Shootable ? 1f : 0f;

        aimIK.solver.IKPositionWeight = Mathf.SmoothDamp(
            aimIK.solver.IKPositionWeight,
            _targetVaule,
            ref _refTargetValue,
            0.25f
        );
    }

    private float currentSpreadRef = 0;
    private void SpreadHandle()
    {
        currentSpread = Mathf.SmoothDamp(currentSpread, 0f, ref currentSpreadRef, 0.5f);
        currentSpread = Mathf.Clamp(currentSpread, 0f, currentGun.GunInfo.Spread);
    }

    public void Fire()
    {
        if (currentGun == null) return;
        pendingFire = true;
    }

    private void FireCallback()
    {
        if (!pendingFire) return;
        pendingFire = false;
        ExecuteFire();
    }

    public void ExecuteFire()
    {
        float xError = MathUtility.SampleGaussian(0f, currentSpread);
        float yError = MathUtility.SampleGaussian(0f, currentSpread);

        currentSpread += 1f / currentGun.GunInfo.Stability;

        Vector3 aimDir = Muzzle.forward;
        aimDir = Quaternion.AngleAxis(yError, Muzzle.up) * aimDir;
        aimDir = Quaternion.AngleAxis(xError, Muzzle.right) * aimDir;

        Quaternion bulletRotation = Quaternion.LookRotation(aimDir);

        //머즐 플래쉬
        EffectPoolManager.SpawnFromPool("MuzzleFlash", Muzzle.position, Muzzle.rotation);

        //총알 발사
        bulletPool.SpawnBullet(
            Muzzle.position,
            bulletRotation,
            1 << gameObject.layer,
            Muzzle.position,                             // shotOrigin
            currentGun.GunInfo.ProjectileSpeed,          // 총알 속도
            currentGun.GunInfo.RoundDamage               // 데미지
        );
    }

    public void SetTarget(Transform target)
    {
        aimTarget = target;
    }

    public void ResetTarget()
    {
        aimTarget = null;
    }

    private void OnDead()
    {
        myIK.enabled = false;
        aimIK.enabled = false;
        myBrain.MotionController.Anim.enabled = false;
    }

    private void OnRevive()
    {
        myIK.enabled = true;
        aimIK.enabled = true;
        myBrain.MotionController.Anim.enabled = true;
    }
}
