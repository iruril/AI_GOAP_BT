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

    private void OnEnable()
    {
        // AimIK solver가 다 돌고 난 뒤 콜백
        if (aimIK != null)
            aimIK.solver.OnPostUpdate += FireCallback;
    }

    private void OnDisable()
    {
        if (aimIK != null)
            aimIK.solver.OnPostUpdate -= FireCallback;
    }

    void Start()
    {
        LoadGun("AK-15");

        myBrain.Sensor.OnTargetSet += SetTarget;
        myBrain.Sensor.OnTargetReset += ResetTarget;
    }

    private void OnDestroy()
    {
        myBrain.Sensor.OnTargetSet -= SetTarget;
        myBrain.Sensor.OnTargetReset -= ResetTarget;

        if (aimIK != null)
            aimIK.solver.OnPostUpdate -= FireCallback;
    }

    void Update()
    {
        AimIKHandle();
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
                    aimTarget.position + Vector3.up * 1.4f
                    : transform.position + transform.forward * 2.0f + Vector3.up * 1.4f;
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

    public void Fire()
    {
        if (currentGun == null) return;
        pendingFire = true;
    }

    private void FireCallback()
    {
        if (!pendingFire) return;
        ExecuteFire();
    }

    public void ExecuteFire()
    {
        pendingFire = false;

        //머즐 플래쉬
        EffectPoolManager.SpawnFromPool("MuzzleFlash", Muzzle.position, Muzzle.rotation);

        //총알 발사
        bulletPool.SpawnBullet(
            Muzzle.position,
            Muzzle.rotation,
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
}
