using UnityEngine;
using System.Collections.Generic;

public class GunHandler : MonoBehaviour
{
    private GOAP.Assualt.AssaultBrain myBrain;

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
    public int CurrentRounds { get; private set; } = 0;

    void Awake()
    {
        myBrain = GetComponent<GOAP.Assualt.AssaultBrain>();
        bulletPool = GetComponent<BulletPool>();
    }

    void Start()
    {
        myBrain.MotionController.FBBIK.solver.leftHandEffector.target = LeftHandIKTarget;
        myBrain.MotionController.FBBIK.solver.leftHandEffector.positionWeight = 1f;
        myBrain.MotionController.AimIK.solver.IKPositionWeight = 0f;

        LoadGun("AK-15");

        myBrain.Sensor.OnTargetSet += SetTarget;
        myBrain.Sensor.OnTargetReset += ResetTarget;
        myBrain.Sensor.MyStat.OnDead += OnDead;
        myBrain.MotionController.AimIK.solver.OnPostUpdate += FireCallback;
    }

    private void OnDestroy()
    {
        myBrain.Sensor.OnTargetSet -= SetTarget;
        myBrain.Sensor.OnTargetReset -= ResetTarget;
        myBrain.Sensor.MyStat.OnDead -= OnDead;
        myBrain.MotionController.AimIK.solver.OnPostUpdate -= FireCallback;
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
        CurrentRounds = currentGun.GunInfo.MagazineCapacity; //임시.

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
        AimIKTargetTransformControl();
        AimIKWeightControl();
    }

    private void AimIKTargetTransformControl()
    {
        AimIKTarget.position = aimTarget != null ?
                    aimTarget.position
                    : transform.position + transform.forward * 3.0f + Vector3.up * 1.2f;
    }

    float _refTargetValue;
    private void AimIKWeightControl()
    {
        float _targetVaule = myBrain.MotionController.Aimable() ? 1f : 0f;

        myBrain.MotionController.AimIK.solver.IKPositionWeight = Mathf.SmoothDamp(
            myBrain.MotionController.AimIK.solver.IKPositionWeight,
            _targetVaule,
            ref _refTargetValue,
            0.1f
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

    private void ExecuteFire()
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

    private void SetTarget(Transform target)
    {
        if (target.TryGetComponent<Animator>(out var animator))
        {
            aimTarget = animator.GetBoneTransform(HumanBodyBones.UpperChest);
        }
        else
        {
            aimTarget = target;
        }
    }

    private void ResetTarget()
    {
        aimTarget = null;
    }

    private void OnDead()
    {
        pendingFire = false;
    }
}
