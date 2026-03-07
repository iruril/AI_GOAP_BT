using UnityEngine;
using System.Collections.Generic;
using MEC;
using System.Linq;
using RootMotion.FinalIK;
using Mirror;
using System;
using Sound;

[System.Serializable]
public struct HitInfo
{
    public Vector3 Point;
    public Quaternion Rotation;
    public string VfxName;
}

public class GunHandler : NetworkBehaviour
{
    #region Events
    public event Action<int> OnRoundChanged;
    public event Action OnFired;
    public event Action<float, float, float, float> OnGunRecoilChanged;
    #endregion

    #region Inspector Settings (Transforms & Audio)
    [Header("Gun 사운드 소스")]
    [SerializeField] private AudioSource audioSource;

    [Header("Gun 트랜스폼 세팅")]
    [SerializeField] Transform gunPos;
    [SerializeField] Transform leftHandIKTarget;
    [SerializeField] Transform leftArmIKHint;
    public Transform LeftHandIKTarget { get { return leftHandIKTarget; } }
    public Transform LeftArmIKHint { get { return leftArmIKHint; } }
    [SerializeField] Transform muzzle;

    [Header("Aim IK Target 세팅")]
    [SerializeField] Transform aimIKStandard;
    [SerializeField] Transform aimIKTarget;

    public Transform Muzzle { get { return muzzle; } }
    public Transform AimIKTarget { get { return aimIKTarget; } }
    public Transform AimIKStandard { get { return aimIKStandard; } }
    #endregion

    #region Network SyncVars & State Variables
    [SyncVar(hook = nameof(OnGunNameChanged))] public string syncedGunName;
    [SyncVar(hook = nameof(OnRoundUpdate))] public int CurrentRounds = 0;
    [SyncVar] public bool IsChamberOpen = false;
    [SyncVar] public bool OnReload;

    private Gun currentGun;
    public Gun CurrentGun { get { return currentGun; } }
    private GameObject currentGunModel;

    private Dictionary<string, (Gun gun, GameObject instance)> gunHistory = new();
    private Dictionary<string, int> roundHistory = new();

    // Strategies
    private Dictionary<FireMode, IFireModeStrategy> fireModeStrategies = new();
    private IGunFireStrategy currentFireStrategy;
    private Dictionary<ReloadType, IGunReloadStrategy> reloadStrategies = new();
    private IGunReloadStrategy currentReloadStrategy;
    private IFireModeStrategy currentFireModeStrategy;
    public FireMode CurrentFireMode => currentFireModeStrategy.Mode;
    
    // Fire & Spread State
    private float lastFireTime = 0f;
    private bool pendingFire = false;
    private float currentSpread = 0; 
    private float currentSpreadRef = 0;
    public float CurrentSpread => currentSpread;
    
    // Hit & Position Buffer
    private List<HitInfo> hitBuffer = new List<HitInfo>();
    private Vector3 clientMuzzlePos;
    private Vector3 clientMuzzleDir;

    // Coroutine Handles
    public CoroutineHandle reloadHandle;
    private CoroutineHandle layerIkHandle;
    private CoroutineHandle spawnBatchHandle;

    private RoomManager rm;
    #endregion

    #region Unity & Mirror Callbacks
    public override void OnStartServer()
    {
        rm = NetworkManager.singleton as RoomManager;
    }

    public override void OnStartLocalPlayer()
    {
        OnRoundChanged += WeaponHUD.Instance.OnRoundChanged;
    }

    public override void OnStopLocalPlayer()
    {
        OnRoundChanged -= WeaponHUD.Instance.OnRoundChanged;
    }

    public override void OnStopServer()
    {
        Timing.KillCoroutines(layerIkHandle);
        Timing.KillCoroutines(reloadHandle);
        Timing.KillCoroutines(spawnBatchHandle);
    }

    public override void OnStopClient()
    {
        Timing.KillCoroutines(layerIkHandle);
    }

    void Update()
    {
        if (!isServer) return;
        SpreadHandle();
    }

    void FixedUpdate()
    {
        if (!isServer) return;

        if (hitBuffer.Count > 0)
        {
            RpcSpawnBatchHitEffects(hitBuffer.ToArray());
            hitBuffer.Clear();
        }
    }
    #endregion

    #region Gun Loading & Weapon Management
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

            WeaponHUD.Instance.OnSelectorChanged(CurrentFireMode);

            OnGunRecoilChanged?.Invoke(
                currentGun.GunInfo.RecoilPitch,
                currentGun.GunInfo.RecoilYawLeft,
                currentGun.GunInfo.RecoilYawRight,
                currentGun.GunInfo.RecoilRoll
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
        currentFireStrategy = FireStrategyFactory.GetStrategy(currentGun.GunInfo.GunType);

        ReloadType rType = currentGun.GunInfo.ReloadType;
        if (!reloadStrategies.ContainsKey(rType))
        {
            reloadStrategies[rType] = ReloadStrategyFactory.CreateStrategy(rType);
        }
        currentReloadStrategy = reloadStrategies[rType];

        FireMode initialMode = FireMode.Single;
        if (currentGun.GunInfo.FireModes != null && currentGun.GunInfo.FireModes.Count > 0)
        {
            initialMode = currentGun.GunInfo.FireModes.Last();
        }

        if (!fireModeStrategies.ContainsKey(initialMode))
        {
            fireModeStrategies[initialMode] = FireModeStrategyFactory.CreateStrategy(initialMode);
        }
        currentFireModeStrategy = fireModeStrategies[initialMode];
        currentFireModeStrategy.ResetState();

        lastFireTime = 0f;
        currentGunModel.SetActive(true);
    }

    [Server]
    private void SaveGun()
    {
        roundHistory[currentGun.GunName] = CurrentRounds;
    }

    [Command]
    public void CmdSwapGun(string gunName)
    {
        if (currentGun != null) SaveGun();
        LoadGun(gunName);
    }

    private void ApplyGunTransforms(Gun gunData)
    {
        gunPos.localPosition = gunData.GunPosition;
        muzzle.localPosition = gunData.MuzzlePosition;
        aimIKStandard.localPosition = gunData.AimStandardPosition;

        leftHandIKTarget.localPosition = gunData.LeftHandIKPosition;
        leftHandIKTarget.localEulerAngles = gunData.LeftHandIKRotation;

        leftArmIKHint.localPosition = gunData.LeftArmIKHint;
    }
    #endregion

    #region Ammo, Spread & Fire Modes
    private void OnRoundUpdate(int oldRounds, int newRounds)
    {
        OnRoundChanged?.Invoke(newRounds);

        if (isLocalPlayer)
        {
            WeaponHUD.Instance.OnRoundChanged(newRounds);
        }
    }

    private void SpreadHandle()
    {
        currentSpread = Mathf.SmoothDamp(currentSpread, 0f, ref currentSpreadRef, 0.5f);
        currentSpread = Mathf.Clamp(currentSpread, 0f, currentGun.GunInfo.Spread);
    }

    public void ToggleFireMode()
    {
        var fireModes = currentGun.GunInfo.FireModes;

        if (fireModes == null || fireModes.Count <= 1) return;

        int currentIndex = fireModes.IndexOf(CurrentFireMode);
        int nextIndex = (currentIndex + 1) % fireModes.Count;
        FireMode nextMode = fireModes[nextIndex];

        if (!fireModeStrategies.ContainsKey(nextMode))
        {
            fireModeStrategies[nextMode] = FireModeStrategyFactory.CreateStrategy(nextMode);
        }

        currentFireModeStrategy = fireModeStrategies[nextMode];
        currentFireModeStrategy.ResetState();

        if (isLocalPlayer)
        {
            WeaponHUD.Instance.OnSelectorChanged(nextMode);
        }
    }

    public void ConsumeAmmo(int amount)
    {
        CurrentRounds = Mathf.Clamp(CurrentRounds - amount, 0, int.MaxValue);
    }

    public void AddSpread(float amount)
    {
        currentSpread += amount;
    }
    #endregion

    #region Shooting & Fire Control
    public void TryFire(bool isPressed, bool isHeld)
    {
        if (OnReload)
        {
            if (isPressed && !isServer)
            {
                CmdTryInterruptReload(isPressed);
            }
            else if (isPressed && isServer)
            {
                currentReloadStrategy.TryInterrupt(this, isPressed); // 호스트일 경우 직접 실행
            }
            return;
        }

        if (CurrentRounds <= 0) return;

        bool isCooldownReady = (Time.time - lastFireTime) >= currentGun.GunInfo.ShotInterval;
        bool canFire = currentFireModeStrategy.CheckCanFire(isPressed, isHeld, isCooldownReady, currentGun.GunInfo);

        if (canFire)
        {
            lastFireTime = Time.time;
            pendingFire = true;
        }
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

        float latency = (float)(NetworkTime.rtt / 2.0);

        ServerExecuteFire(pos, dir, latency);
    }

    [Server]
    public void FireCallback()
    {
        if (!isServer) return;
        if (!pendingFire) return;

        pendingFire = false;

        Vector3 pos = muzzle.position;
        Vector3 dir = muzzle.forward;

        ServerExecuteFire(pos, dir, 0);
    }

    [Server]
    private void ServerExecuteFire(Vector3 muzzlePos, Vector3 muzzleDir, float lagTime)
    {
        if ((Muzzle.position - muzzlePos).sqrMagnitude > 2f) return;
        if (CurrentRounds <= 0) return;

        currentFireStrategy.ExecuteFire(this, muzzlePos, muzzleDir, lagTime);
    }

    public void SpawnAndBroadcastBullet(Vector3 muzzlePos, Vector3 finalDir, float lagTime)
    {
        Quaternion bulletRotation = Quaternion.LookRotation(finalDir);
        int ignoreLayerMask = rm.FriendlyFire ? 0 : 1 << gameObject.layer;
        float speed = currentGun.GunInfo.ProjectileSpeed;
        float damage = currentGun.GunInfo.RoundDamage;
        float headMultiplier = currentGun.GunInfo.HeadDamageMultiplier;

        BulletPool.SpawnBullet(muzzlePos, bulletRotation, ignoreLayerMask, muzzlePos, speed, damage, headMultiplier, lagTime, this);
        RpcSpawnBullet(muzzlePos, bulletRotation, ignoreLayerMask, muzzlePos, speed, damage, headMultiplier, lagTime);
    }

    [ClientRpc]
    private void RpcSpawnBullet(Vector3 position, Quaternion rotation, LayerMask myTeamLayer, Vector3 origin,
        float projectileSpeed, float damage, float headMultiplier, float lagTime)
    {
        if (isServer) return;

        BulletPool.SpawnBullet(
            position,
            rotation,
            myTeamLayer,
            origin,
            projectileSpeed,
            damage,
            headMultiplier,
            lagTime
        );
    }
    #endregion

    #region Hit Detection & VFX
    [ClientRpc]
    public void RpcPlayMuzzleFlash(Vector3 muzzlePos, Quaternion rot)
    {
        EffectPoolManager.SpawnFromPool("MuzzleFlash", muzzlePos, rot);
        SoundManager.Instance.PlaySound(currentGun.GunInfo.SoundClipID, audioSource, 1.0f);
        if (isLocalPlayer) OnFired?.Invoke();
    }

    [Server]
    public void ServerReportHit(Vector3 point, Quaternion rot, string vfxName)
    {
        hitBuffer.Add(new HitInfo
        {
            Point = point,
            Rotation = rot,
            VfxName = vfxName
        });
    }

    [ClientRpc]
    private void RpcSpawnBatchHitEffects(HitInfo[] hits)
    {
        if (hits.Length > 3)
        {
            spawnBatchHandle = Timing.RunCoroutine(SpawnBatchRoutine(hits));
        }
        else
        {
            for (int i = 0; i < hits.Length; i++)
            {
                SpawnEffect(hits[i]);
            }
        }
    }

    private IEnumerator<float> SpawnBatchRoutine(HitInfo[] hits)
    {
        for (int i = 0; i < hits.Length; i++)
        {
            SpawnEffect(hits[i]);

            if (i % 3 == 0) yield return Timing.WaitForOneFrame;
        }
    }

    private void SpawnEffect(HitInfo hit)
    {
        EffectPoolManager.SpawnFromPool(hit.VfxName, hit.Point, hit.Rotation);
    }
    #endregion

    #region Reloading & IK Animation
    public void Reload()
    {
        if (OnReload) return;
        OnReload = true;

        if (isServer) // 서버 authority
        {
            StartReloadServerSide();
        }
        else // 클라이언트 authority
        {
            CmdRequestReload();
        }
    }

    [Command]
    private void CmdRequestReload()
    {
        if (OnReload) return;

        double startTime = NetworkTime.time;
        reloadHandle = Timing.RunCoroutine(currentReloadStrategy.ExecuteReload(this, startTime));
    }

    [Server]
    private void StartReloadServerSide()
    {
        double startTime = NetworkTime.time;
        reloadHandle = Timing.RunCoroutine(currentReloadStrategy.ExecuteReload(this, startTime));
    }

    [Command]
    public void CmdTryInterruptReload(bool isPressed)
    {
        currentReloadStrategy.TryInterrupt(this, isPressed);
    }

    [ClientRpc]
    public void RpcUpdateReloadAnimation(int animHash, float targetIK, float targetLayer, double serverTime, float blendDuration)
    {
        if (isServer) return;
        PerformReloadAnimation(animHash, targetIK, targetLayer, serverTime, blendDuration);
    }

    public void PerformReloadAnimation(int animHash, float targetIK, float targetLayer, double serverTime, float blendDuration)
    {
        Animator anim = GetComponent<Animator>();
        IKEffector leftHand = GetComponent<RootMotion.FinalIK.FullBodyBipedIK>().solver.leftHandEffector;
        IKConstraintBend leftBend = GetComponent<RootMotion.FinalIK.FullBodyBipedIK>().solver.GetBendConstraint(FullBodyBipedChain.LeftArm);

        if (animHash != 0)
        {
            anim.CrossFade(animHash, 0.2f, 1, 0f);
        }

        double now = NetworkTime.time;
        float elapsed = (float)(now - serverTime);

        Timing.KillCoroutines(layerIkHandle);
        layerIkHandle = Timing.RunCoroutine(LerpIKAndLayer(anim, leftHand, leftBend, targetIK, targetLayer, blendDuration, elapsed));
    }

    private IEnumerator<float> LerpIKAndLayer(Animator anim, IKEffector leftHand, IKConstraintBend leftBend,
        float targetIK, float targetLayer, float duration, float startOffset)
    {
        float t = Mathf.Clamp(startOffset, 0f, duration);

        if (t >= duration)
        {
            leftHand.positionWeight = targetIK;
            leftHand.rotationWeight = targetIK;
            leftBend.weight = targetIK;
            anim.SetLayerWeight(1, targetLayer);
            yield break;
        }

        float k0 = t / duration;

        float startIK = Mathf.Lerp(leftHand.positionWeight, targetIK, k0);
        float startLayer = Mathf.Lerp(anim.GetLayerWeight(1), targetLayer, k0);

        while (t < duration)
        {
            t += Timing.DeltaTime;
            float k = Mathf.Clamp01(t / duration);
            float lerpT = Mathf.Lerp(startIK, targetIK, k);
            leftHand.positionWeight = lerpT;
            leftHand.rotationWeight = lerpT;
            leftBend.weight = lerpT;
            anim.SetLayerWeight(1, Mathf.Lerp(startLayer, targetLayer, k));

            yield return Timing.WaitForOneFrame;
        }

        leftHand.positionWeight = targetIK;
        leftHand.rotationWeight = targetIK;
        leftBend.weight = targetIK;
        anim.SetLayerWeight(1, targetLayer);
    }
    #endregion

    #region Player State Management
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
    #endregion
}