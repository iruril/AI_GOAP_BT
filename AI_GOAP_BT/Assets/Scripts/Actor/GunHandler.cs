using UnityEngine;
using System.Collections.Generic;
using MEC;
using System.Linq;
using RootMotion.FinalIK;

public class GunHandler : MonoBehaviour
{
    [Header("Gun 트랜스폼 세팅")]
    [SerializeField] Transform gunPos;
    [SerializeField] Transform leftHandIKTarget;
    public Transform LeftHandIKTarget { get { return leftHandIKTarget; } }
    [SerializeField] Transform muzzle;

    [Header("Aim IK Target 세팅")]
    [SerializeField] Transform aimIKTarget;
    public Transform AimIKTarget { get { return aimIKTarget; } }

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
        bulletPool = GetComponent<BulletPool>();
    }

    void Start()
    {
        LoadGun("AK-15");
    }

    void Update()
    {
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

        currentGunModel.transform.SetParent(gunPos, false);
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
        gunPos.localPosition = gunData.GunPosition;
        muzzle.localPosition = gunData.MuzzlePosition;

        leftHandIKTarget.localPosition = gunData.LeftHandIKPosition;
        leftHandIKTarget.localEulerAngles = gunData.LeftHandIKRotation;
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

    public void FireCallback()
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

        Vector3 aimDir = muzzle.forward;
        aimDir = Quaternion.AngleAxis(yError, muzzle.up) * aimDir;
        aimDir = Quaternion.AngleAxis(xError, muzzle.right) * aimDir;

        Quaternion bulletRotation = Quaternion.LookRotation(aimDir);

        //머즐 플래쉬
        EffectPoolManager.SpawnFromPool("MuzzleFlash", muzzle.position, muzzle.rotation);

        //총알 발사
        bulletPool.SpawnBullet(
            muzzle.position,
            bulletRotation,
            1 << gameObject.layer,
            muzzle.position,                             // shotOrigin
            currentGun.GunInfo.ProjectileSpeed,          // 총알 속도
            currentGun.GunInfo.RoundDamage               // 데미지
        );
    }

    public void OnDead()
    {
        pendingFire = false;
        OnReload = false;
        Timing.KillCoroutines(reloadHandle); 
        
        foreach (var key in roundHistory.Keys.ToList())
        {
            roundHistory[key] = gunHistory[key].gun.GunInfo.MagazineCapacity;
        }
        CurrentRounds = currentGun.GunInfo.MagazineCapacity;
    }

    public void Reload(Animator anim, IKEffector leftHand)
    {
        reloadHandle = Timing.RunCoroutine(ReloadRoutine(anim, leftHand));
    }

    private IEnumerator<float> ReloadRoutine(Animator anim, IKEffector leftHand)
    {
        StartReload(anim, leftHand);
        yield return Timing.WaitForSeconds(1.9f);
        CompleteReload(anim, leftHand);
    }

    private void StartReload(Animator anim, IKEffector leftHand)
    {
        OnReload = true;
        Timing.RunCoroutine(LerpIKAndLayer(anim, leftHand, 0f, 1f, 0.15f));
        anim.CrossFade(AnimHash.Reload, 0.1f);
    }

    private void CompleteReload(Animator anim, IKEffector leftHand)
    {
        Timing.RunCoroutine(LerpIKAndLayer(anim, leftHand, 1f, 0f, 0.15f));
        CurrentRounds = CurrentRounds == 0
            ? currentGun.GunInfo.MagazineCapacity
            : currentGun.GunInfo.MagazineCapacity + 1;
        OnReload = false;
    }

    private IEnumerator<float> LerpIKAndLayer(Animator anim, IKEffector leftHand,
        float targetIK, float targetLayer, float duration)
    {
        float t = 0f;

        float startIK = leftHand.positionWeight;
        float startLayer = anim.GetLayerWeight(1);

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;

            leftHand.positionWeight =
                Mathf.Lerp(startIK, targetIK, k);

            anim.SetLayerWeight(
                1,
                Mathf.Lerp(startLayer, targetLayer, k)
            );

            yield return Timing.WaitForOneFrame;
        }

        leftHand.positionWeight = targetIK;
        anim.SetLayerWeight(1, targetLayer);
    }
}
