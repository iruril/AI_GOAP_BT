using UnityEngine;
using System.Collections.Generic;
using MEC;
using System.Linq;

public class GunHandler : MonoBehaviour
{
    private GOAP.Assualt.AssaultBrain myBrain;

    [Header("Gun 트랜스폼 세팅")]
    [SerializeField] Transform GunPos;
    [SerializeField] Transform LeftHandIKTarget;
    [SerializeField] Transform Muzzle;

    [Header("Aim IK Target 세팅")]
    [SerializeField] Transform AimIKTarget;

    private Gun currentGun;
    public Gun CurrentGun { get { return currentGun; } }
    private GameObject currentGunModel;

    private BulletPool bulletPool;

    private Dictionary<string, (Gun gun, GameObject instance)> gunHistory = new();
    private Dictionary<string, int> roundHistory = new();
    
    private bool pendingFire = false;

    private float currentSpread = 0;
    public int CurrentRounds { get; private set; } = 0;
    public bool OnReload { get; private set; }
    CoroutineHandle reloadHandle;

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

        myBrain.Sensor.MyStat.OnDead += OnDead;
        myBrain.MotionController.AimIK.solver.OnPostUpdate += FireCallback;
    }

    private void OnDestroy()
    {
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
        }

        currentGun = gunData.gun;

        if (currentGunModel != null)
            currentGunModel.SetActive(false);

        if (!cached)
        {
            GameObject gunModel = Instantiate(gunData.instance);
            gunHistory.Add(gunName, (gunData.gun, gunModel));
            currentGunModel = gunModel;
            roundHistory.Add(gunName, gunData.gun.GunInfo.MagazineCapacity);
        }
        else
        {
            currentGunModel = gunHistory[gunName].instance;
        }

        currentGunModel.transform.SetParent(GunPos, false);
        currentGunModel.transform.localPosition = Vector3.zero;
        currentGunModel.transform.localRotation = Quaternion.identity;
        currentSpread = 0;
        CurrentRounds = roundHistory[currentGun.GunName];

        ApplyGunTransforms(currentGun);
    }

    void SaveGun()
    {
        roundHistory[currentGun.GunName] = CurrentRounds;
    }

    public void SwapGun(string gunName)
    {
        if (currentGun != null) SaveGun();
        LoadGun(gunName);
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
        AimIKTarget.position =
            (myBrain.Sensor.HasTarget && myBrain.Sensor.HasLastSeenPosition)
            ? myBrain.Sensor.LastSeenPosition
            : transform.position + transform.forward * 20f + Vector3.up * 1.2f;
    }

    float _refTargetValue;
    private void AimIKWeightControl()
    {
        float _targetVaule = myBrain.MotionController.Aimable() && !OnReload ? 1f : 0f;

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
        if (CurrentRounds == 0) return;
        pendingFire = true;
    }

    private void FireCallback()
    {
        if (CurrentRounds == 0) return;
        if (!pendingFire) return;
        pendingFire = false;
        ExecuteFire();
    }

    private void ExecuteFire()
    {
        if (CurrentRounds == 0) return;
        CurrentRounds = Mathf.Clamp(CurrentRounds - 1, 0, int.MaxValue);

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

    private void OnDead()
    {
        pendingFire = false;
        Timing.KillCoroutines(reloadHandle); 
        
        foreach (var key in roundHistory.Keys.ToList())
        {
            roundHistory[key] = gunHistory[key].gun.GunInfo.MagazineCapacity;
        }
        CurrentRounds = currentGun.GunInfo.MagazineCapacity;
    }

    public void Reload()
    {
        reloadHandle = Timing.RunCoroutine(ReloadRoutine());
    }

    private IEnumerator<float> ReloadRoutine()
    {
        StartReload();
        yield return Timing.WaitForSeconds(2f);
        CompleteReload();
    }

    private void StartReload()
    {
        OnReload = true;
        Timing.RunCoroutine(LerpIKAndLayer(0f, 1f, 0.15f));
        myBrain.MotionController.Anim.CrossFade(AnimHash.Reload, 0.1f);
    }

    private void CompleteReload()
    {
        Timing.RunCoroutine(LerpIKAndLayer(1f, 0f, 0.15f));
        CurrentRounds = CurrentRounds == 0
            ? currentGun.GunInfo.MagazineCapacity
            : currentGun.GunInfo.MagazineCapacity + 1;
        OnReload = false;
    }

    private IEnumerator<float> LerpIKAndLayer(float targetIK, float targetLayer, float duration)
    {
        float t = 0f;

        float startIK = myBrain.MotionController.FBBIK.solver.leftHandEffector.positionWeight;
        float startLayer = myBrain.MotionController.Anim.GetLayerWeight(1);

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;

            myBrain.MotionController.FBBIK.solver.leftHandEffector.positionWeight =
                Mathf.Lerp(startIK, targetIK, k);

            myBrain.MotionController.Anim.SetLayerWeight(
                1,
                Mathf.Lerp(startLayer, targetLayer, k)
            );

            yield return Timing.WaitForOneFrame;
        }

        myBrain.MotionController.FBBIK.solver.leftHandEffector.positionWeight = targetIK;
        myBrain.MotionController.Anim.SetLayerWeight(1, targetLayer);
    }
}
