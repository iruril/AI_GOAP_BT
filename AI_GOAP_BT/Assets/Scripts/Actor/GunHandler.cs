using UnityEngine;
using System.Collections.Generic;
using MEC;
using System.Linq;
using RootMotion.FinalIK;
using Mirror;
using System;
using System.Collections;

public class GunHandler : NetworkBehaviour
{
    public event Action<int> OnRoundChanged;
    public event Action OnGunChanged;

    [Header("Gun 트랜스폼 세팅")]
    [SerializeField] Transform gunPos;
    [SerializeField] Transform leftHandIKTarget;
    public Transform LeftHandIKTarget { get { return leftHandIKTarget; } }
    [SerializeField] Transform muzzle;

    [Header("Aim IK Target 세팅")]
    [SerializeField] Transform aimIKStandard;
    [SerializeField] Transform aimIKTarget;
    public Transform AimIKTarget { get { return aimIKTarget; } }

    [SyncVar(hook = nameof(OnGunNameChanged))] public string syncedGunName;
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
    [SyncVar(hook = nameof(OnRoundUpdate))] public int CurrentRounds = 0;
    [SyncVar] public bool OnReload;
    CoroutineHandle reloadHandle;

    void Awake()
    {
        bulletPool = GetComponent<BulletPool>();
    }

    public override void OnStartServer()
    {
    }

    public override void OnStartLocalPlayer()
    {
        OnRoundChanged += WeaponHUD.Instance.OnRoundChanged;
    }

    public override void OnStopLocalPlayer()
    {
        OnRoundChanged -= WeaponHUD.Instance.OnRoundChanged;
    }

    void Update()
    {
        if (!isServer) return;
        SpreadHandle();
    }

    [Server]
    public void LoadGun(string gunName)
    {
        LoadGunVisual(gunName);

        if (!roundHistory.ContainsKey(gunName))
            roundHistory.Add(gunName, currentGun.GunInfo.MagazineCapacity);

        CurrentRounds = roundHistory[gunName];
        syncedGunName = gunName;
    }

    private void OnGunNameChanged(string oldName, string newName)
    {
        LoadGunVisual(newName);

        if (isLocalPlayer)
        {
            WeaponHUD.Instance.OnGunChanged(
                currentGun.GunName,
                currentGun.GunInfo.MagazineCapacity + 1
            );
        }
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
        currentGunModel.SetActive(true);
    }

    [Server]
    void SaveGun()
    {
        roundHistory[currentGun.GunName] = CurrentRounds;
    }

    [Command]
    public void CmdSwapGun(string gunName)
    {
        if (currentGun != null) SaveGun();
        LoadGun(gunName);
    }

    void ApplyGunTransforms(Gun gunData)
    {
        gunPos.localPosition = gunData.GunPosition;
        muzzle.localPosition = gunData.MuzzlePosition;
        aimIKStandard.localPosition = gunData.AimStandardPosition;

        leftHandIKTarget.localPosition = gunData.LeftHandIKPosition;
        leftHandIKTarget.localEulerAngles = gunData.LeftHandIKRotation;
    }

    private void OnRoundUpdate(int oldRounds, int newRounds)
    {
        OnRoundChanged?.Invoke(newRounds);

        if (isLocalPlayer)
        {
            WeaponHUD.Instance.OnRoundChanged(newRounds);
        }
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

    public void ClientFireCallback()
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

        float spreadRad = currentSpread * Mathf.Deg2Rad;
        Vector2 error = MathUtility.SampleGaussian2D(spreadRad); 
        Vector3 localDir = new Vector3(error.x, error.y, 1f);
        localDir.Normalize();
        Quaternion basis = Quaternion.LookRotation(muzzleDir);
        Vector3 finalDir = basis * localDir;

        Quaternion bulletRotation = Quaternion.LookRotation(finalDir); 
        int teamLayer = 1 << gameObject.layer;
        float speed = currentGun.GunInfo.ProjectileSpeed;
        float damage = currentGun.GunInfo.RoundDamage;

        bulletPool.SpawnBullet(
            this,
            muzzlePos,
            bulletRotation,
            teamLayer,
            muzzlePos,      // shotOrigin
            speed,          // 총알 속도
            damage          // 데미지
        );

        RpcSpawnBullet(
            muzzlePos,
            bulletRotation,
            teamLayer,
            muzzlePos,      // shotOrigin
            speed,          // 총알 속도
            damage          // 데미지
        );

        RpcPlayMuzzleFlash(muzzlePos, Quaternion.LookRotation(muzzleDir));

        currentSpread += 1f / currentGun.GunInfo.Stability;
    }

    [ClientRpc]
    private void RpcSpawnBullet(
        Vector3 position,
        Quaternion rotation,
        LayerMask myTeamLayer,
        Vector3 origin,
        float projectileSpeed,
        float damage)
    {
        if (isServer) return;

        bulletPool.SpawnBullet(
            position,
            rotation,
            myTeamLayer,
            origin,
            projectileSpeed,
            damage
        );
    }

    [ClientRpc]
    private void RpcPlayMuzzleFlash(Vector3 muzzlePos, Quaternion rot)
    {
        EffectPoolManager.SpawnFromPool("MuzzleFlash", muzzlePos, rot);
    }

    [Server]
    public void ServerReportHit(Vector3 point, Quaternion rot, string vfxName)
    {
        RpcSpawnHitEffect(point, rot, vfxName);
    }

    [ClientRpc]
    private void RpcSpawnHitEffect(Vector3 point, Quaternion rot, string vfxName)
    {
        EffectPoolManager.SpawnFromPool(vfxName, point, rot);
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

    public void Reload()
    {
        if (OnReload) return;

        if (isServer)
            reloadHandle = Timing.RunCoroutine(ServerReloadRoutine());
        else
            CmdRequestReload();
    }

    [Command]
    private void CmdRequestReload()
    {
        reloadHandle = Timing.RunCoroutine(ServerReloadRoutine());
    }

    private IEnumerator<float> ServerReloadRoutine()
    {
        OnReload = true;

        if (isServer) //Host
        {
            Animator anim = GetComponent<Animator>();
            IKEffector leftHand = GetComponent<RootMotion.FinalIK.FullBodyBipedIK>().solver.leftHandEffector;
            anim.CrossFade(AnimHash.Reload, 0.1f);
            Timing.RunCoroutine(LerpIKAndLayer(anim, leftHand, 0f, 1f, 0.15f));
        }

        RpcStartReload();

        yield return Timing.WaitForSeconds(1.9f);

        int newRounds = (CurrentRounds == 0)
            ? currentGun.GunInfo.MagazineCapacity
            : currentGun.GunInfo.MagazineCapacity + 1;

        CurrentRounds = newRounds;

        OnReload = false;

        if (isServer) //Host
        {
            Animator anim = GetComponent<Animator>();
            IKEffector leftHand = GetComponent<RootMotion.FinalIK.FullBodyBipedIK>().solver.leftHandEffector;
            Timing.RunCoroutine(LerpIKAndLayer(anim, leftHand, 1f, 0f, 0.15f));
        }

        RpcCompleteReload();
    }

    [ClientRpc]
    private void RpcStartReload()
    {
        if (isServer) return;

        Animator anim = GetComponent<Animator>();
        IKEffector leftHand = GetComponent<RootMotion.FinalIK.FullBodyBipedIK>().solver.leftHandEffector;

        anim.CrossFade(AnimHash.Reload, 0.1f);
        Timing.RunCoroutine(LerpIKAndLayer(anim, leftHand, 0f, 1f, 0.15f));
    }

    [ClientRpc]
    private void RpcCompleteReload()
    {
        if (isServer) return;

        Animator anim = GetComponent<Animator>();
        IKEffector leftHand = GetComponent<RootMotion.FinalIK.FullBodyBipedIK>().solver.leftHandEffector;
        Timing.RunCoroutine(LerpIKAndLayer(anim, leftHand, 1f, 0f, 0.15f));
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
