using UnityEngine;
using System.Collections.Generic;
using MEC;
using System.Linq;
using RootMotion.FinalIK;
using Mirror;

public class GunHandler : NetworkBehaviour
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
    
    // 플레이어용: 클라이언트가 계산한 muzzle 정보
    private Vector3 clientMuzzlePos;
    private Vector3 clientMuzzleDir;

    private float currentSpread = 0;
    [SyncVar] public int CurrentRounds = 0;
    public bool OnReload { get; private set; }
    CoroutineHandle reloadHandle;

    void Awake()
    {
        bulletPool = GetComponent<BulletPool>();
    }

    void Start()
    {
        if (isServer)
            LoadGun("AK-15");
    }

    void Update()
    {
        if (!isServer) return;
        SpreadHandle();
    }

    [Server]
    private void LoadGun(string gunName)
    {
        LoadGunVisual(gunName);

        if (!roundHistory.ContainsKey(gunName))
            roundHistory.Add(gunName, currentGun.GunInfo.MagazineCapacity);

        CurrentRounds = roundHistory[gunName];

        RpcLoadGun(gunName);
    }

    [ClientRpc]
    private void RpcLoadGun(string gunName)
    {
        if (isServer) return;
        LoadGunVisual(gunName);
    }

    private void LoadGunVisual(string gunName)
    {
        bool cached = gunHistory.ContainsKey(gunName);
        (Gun gun, GameObject instance) gunData;

        if (cached)
            gunData = gunHistory[gunName];
        else
            gunData = GameManager.GetInstance().GunTable[gunName];

        currentGun = gunData.gun;

        if (currentGunModel != null)
            currentGunModel.SetActive(false);

        if (!cached)
        {
            GameObject model = Instantiate(gunData.instance);
            gunHistory.Add(gunName, (gunData.gun, model));
            currentGunModel = model;
        }
        else
        {
            currentGunModel = gunHistory[gunName].instance;
        }

        currentGunModel.transform.SetParent(gunPos, false);
        currentGunModel.transform.localPosition = Vector3.zero;
        currentGunModel.transform.localRotation = Quaternion.identity;

        ApplyGunTransforms(currentGun);
    }

    void SaveGun()
    {
        roundHistory[currentGun.GunName] = CurrentRounds;
    }

    [Command]
    public void CmdSwapGun(string gunName)
    {
        SwapGun(gunName);
    }

    private void SwapGun(string gunName)
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
        if (CurrentRounds <= 0) return;
        pendingFire = true;
    }

    public void LocalFireCallback()
    {
        if (!pendingFire) return;
        pendingFire = false;

        clientMuzzlePos = muzzle.position;
        clientMuzzleDir = muzzle.forward;

        CmdFire(clientMuzzlePos, clientMuzzleDir);
    }

    [Command]
    private void CmdFire(Vector3 pos, Vector3 dir)
    {
        if (CurrentRounds <= 0) return;
        ServerExecuteFire(pos, dir);
    }

    public void FireCallback()
    {
        if (!isServer) return;
        if (!pendingFire) return;

        pendingFire = false;

        Vector3 pos = muzzle.position;
        Vector3 dir = muzzle.forward;

        ServerExecuteFire(pos, dir);
    }

    [Server]
    private void ServerExecuteFire(Vector3 muzzlePos, Vector3 muzzleDir)
    {
        if (CurrentRounds == 0) return;
        CurrentRounds = Mathf.Clamp(CurrentRounds - 1, 0, int.MaxValue);

        float xError = MathUtility.SampleGaussian(0f, currentSpread);
        float yError = MathUtility.SampleGaussian(0f, currentSpread);

        currentSpread += 1f / currentGun.GunInfo.Stability; 
        
        Vector3 right = Vector3.Cross(Vector3.up, muzzleDir).normalized;
        Vector3 up = Vector3.Cross(muzzleDir, right).normalized;

        Vector3 finalDir = muzzleDir;
        finalDir = Quaternion.AngleAxis(yError, up) * muzzleDir;
        finalDir = Quaternion.AngleAxis(xError, right) * muzzleDir;

        Quaternion bulletRotation = Quaternion.LookRotation(finalDir);

        //총알 발사
        bulletPool.SpawnBullet(
            this,
            muzzlePos,
            bulletRotation,
            1 << gameObject.layer,
            muzzlePos,                                   // shotOrigin
            currentGun.GunInfo.ProjectileSpeed,          // 총알 속도
            currentGun.GunInfo.RoundDamage               // 데미지
        );

        RpcPlayMuzzleFlash(muzzlePos, Quaternion.LookRotation(muzzleDir));
    }

    [ClientRpc]
    private void RpcPlayMuzzleFlash(Vector3 muzzlePos, Quaternion rot)
    {
        EffectPoolManager.SpawnFromPool("MuzzleFlash", muzzlePos, rot);
    }

    [Server]
    public void ServerReportHit(Vector3 point, Vector3 normal)
    {
        RpcSpawnHitEffect(point, normal);
    }

    [ClientRpc]
    private void RpcSpawnHitEffect(Vector3 point, Vector3 normal)
    {
        EffectPoolManager.SpawnFromPool("Hit", point, Quaternion.LookRotation(normal));
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
