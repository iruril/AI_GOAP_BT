using UnityEngine;
using System;
using System.Collections.Generic;
using MEC;
using Mirror;

[Serializable]
public struct KDA
{
    public int Kills;
    public int Assists;
    public int Deaths;
}

[Serializable]
public struct DamageRecord
{
    public uint attackerNetId;
    public HitBox.HitBoxType hitBoxType;
    public float damage;
    public string gunName;

    public DamageRecord(uint attackerNetId, HitBox.HitBoxType hitBoxType, float damage, string gunName)
    {
        this.attackerNetId = attackerNetId;
        this.hitBoxType = hitBoxType;
        this.damage = damage;
        this.gunName = gunName;
    }
}

public class Stat : NetworkBehaviour, IDamageable, IChatSender
{
    public event Action OnDead;
    public event Action OnRevive;
    public event Action<Vector3, LayerMask> OnGrazeBullet;

    [SyncVar(hook = nameof(OnTeamChanged))]
    public Team MyTeam = Team.Blue;

    [SyncVar(hook = nameof(OnNicknameChanged))]
    public string Nickname;

    ActorUIMarker marker;

    string IChatSender.Nickname => Nickname;
    Team IChatSender.MyTeam => MyTeam;
    uint IChatSender.NetId => netId;

    [SerializeField] private float maxHP = 100f;
    public float MaxHP => maxHP;

    [SyncVar(hook = nameof(OnHPChanged))] public float CurrentHP;
    [SyncVar(hook = nameof(OnDeathStateChanged))] public bool IsDead = false;

    [SyncVar(hook = nameof(OnKDAChanged))]
    public KDA CurrentKDA = new();

    [SyncVar]
    public Vector3 ServerVelocity; 

    private Vector3 prevPosition;
    private Vector3 nextPosition;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    public Vector3 SpawnPosition => spawnPosition;
    public Quaternion SpawnRotation => spawnRotation;
    public Vector3 LastDeadPosition { get; private set; }

    public CapturePoint.CapturePoint CurrentCapture { get; set; } = null;

    private float lastDamageTime = -999f;
    private CoroutineHandle hpRegenHandle;
    private CoroutineHandle respawnHandle;

    [NonSerialized] private List<DamageRecord> damageRecords = new();
    [NonSerialized] private readonly HashSet<uint> assistBuffer = new();

    private const float NO_DAMAGE_DURATION = 5f;
    private const float REGEN_RATE = 0.1f; 
    private const int MAX_DAMAGE_RECORDS = 10;

    private RoomManager roomManager;

    private void Awake()
    {
        marker = GetComponent<ActorUIMarker>();
    }

    public override void OnStartServer()
    {
        roomManager = NetworkManager.singleton as RoomManager;
        InitHP();
        hpRegenHandle = Timing.RunCoroutine(HPRegenHandle());

        prevPosition = transform.position;
        nextPosition = transform.position;
        ServerVelocity = Vector3.zero;

        SetTeam(MyTeam);
        if (MyTeam == Team.Blue)
        {
            GetComponent<GunHandler>().LoadGun("MPX");
        }
        else
        {
            GetComponent<GunHandler>().LoadGun("AK-12");
        }
    }

    public override void OnStartClient()
    {
        if (!isLocalPlayer)
        {
            marker.Marker.enabled = true;
            marker.Nickname.enabled = true;
            marker.SetColor(MyTeam);
            marker.SetNickname(Nickname);
        }

        OnTeamChanged(MyTeam, MyTeam);
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        ScoreboardHUD.Instance?.AddUser(Nickname, netId, MyTeam == Team.Blue);
    }

    public override void OnStopClient()
    {
        ScoreboardHUD.Instance?.RemoveUser(netId);
    }

    public override void OnStartLocalPlayer()
    {
        HealthGuageHUD.Instance.SetNickname(Nickname);
        marker.SetDisable();
    }

    public override void OnStopServer()
    {
        Timing.KillCoroutines(respawnHandle);
        Timing.KillCoroutines(hpRegenHandle);
    }

    private void InitHP()
    {
        CurrentHP = MaxHP;
    }

    private void FixedUpdate()
    {
        if(isServer) RecordServerVelocity();
    }

    [Server]
    private void RecordServerVelocity()
    {
        nextPosition = transform.position;

        if (prevPosition != Vector3.zero)
        {
            ServerVelocity = (nextPosition - prevPosition) / Time.fixedDeltaTime;
        }
        else
        {
            ServerVelocity = Vector3.zero;
        }

        prevPosition = nextPosition;
    }

    #region Damageable Field
    [Server]
    public virtual void ApplyDamage(float dmg, Vector3 shotOrigin, Vector3 hitPoint)
    {
        if (IsDead) return;

        CurrentHP -= dmg;
        lastDamageTime = Time.time;

        if (CurrentHP <= 0f && !IsDead)
        {
            Die();
            respawnHandle = Timing.RunCoroutine(Respawn());
        }
    }

    public void OnGraze(Vector3 shotOrigin, LayerMask bulletOwnerLayer)
    {
        if (isServer)
        {
            if(authority) OnGrazeBullet?.Invoke(shotOrigin, bulletOwnerLayer);
        }
        else if (isLocalPlayer)
        {
            OnGrazeBullet?.Invoke(shotOrigin, bulletOwnerLayer);
        }
    }

    [Server]
    public void AddDamageRecord(uint attackerNetId, float damage, HitBox.HitBoxType hitBoxType, string gunName)
    {
        if (IsDead) return;

        if (NetworkServer.spawned.TryGetValue(attackerNetId, out NetworkIdentity attackerIdentity))
        {
            if (damageRecords.Count >= MAX_DAMAGE_RECORDS)
            {
                damageRecords.RemoveAt(0);
            }

            damageRecords.Add(new DamageRecord(attackerNetId, hitBoxType, damage, gunName));

            if (attackerIdentity.connectionToClient != null)
            {
                bool isFatalHit = CurrentHP - damage <= 0f;
                TargetReceiveHitFeedback(attackerIdentity.connectionToClient, netId, damage, isFatalHit);
            }
        }
    }

    [TargetRpc]
    private void TargetReceiveHitFeedback(NetworkConnection target, uint victimNetId, float damage, bool isKilled)
    {
        InGameUI.Instance?.PlayHitMark(isKilled);
        DamageStackUI.Instance?.PopDamageStack(victimNetId, damage, isKilled);
    }
    #endregion

    private IEnumerator<float> HPRegenHandle()
    {
        while (true)
        {
            yield return Timing.WaitForSeconds(0.1f);

            if (IsDead) continue;

            // 최근 피해 이후 5초가 지났으면 회복
            if (Time.time - lastDamageTime >= NO_DAMAGE_DURATION)
            {
                float regenAmount = MaxHP * REGEN_RATE * 0.1f;
                CurrentHP = Mathf.Min(CurrentHP + regenAmount, MaxHP);
            }

            if (CurrentHP >= MaxHP)
            {
                CurrentHP = MaxHP;
                if (damageRecords.Count > 0) damageRecords.Clear(); // 풀피면 기록 삭제
            }
        }
    }

    private void Die()
    {
        CurrentHP = 0f;
        IsDead = true;

        AddDeath();

        ProcessKillAndAssist();

        CurrentCapture?.RemoveIntruder(this);
    }

    [Server]
    private void ProcessKillAndAssist()
    {
        if (damageRecords.Count == 0) return;

        DamageRecord killRecord = damageRecords[^1];
        uint killerNetId = killRecord.attackerNetId;
        bool isHeadshotKill = killRecord.hitBoxType == HitBox.HitBoxType.Head;

        if (NetworkServer.spawned.TryGetValue(killerNetId, out var killerIdentity))
        {
            var killerStat = killerIdentity.GetComponent<Stat>();
            if (killerStat != null)
            {
                bool isEnemy = IsEnemy(killerStat);
                if (isEnemy)
                {
                    killerStat.AddKill();
                    GameFlowManager.Instance.ApplyKillScore(killerStat.MyTeam, MyTeam);
                }

                LogManager.Instance.ReportKill(
                    killerNetId,
                    netId,
                    killerStat.MyTeam == Team.Blue,
                    MyTeam == Team.Blue,
                    killRecord.hitBoxType == HitBox.HitBoxType.Head,
                    killRecord.gunName
                );
            }
        }

        ProcessAssistByDamageLog(killerNetId);
        damageRecords.Clear();
    }

    [Server]
    private void ProcessAssistByDamageLog(uint killerNetId)
    {
        assistBuffer.Clear();
        float accumulatedDamage = 0f;

        for (int i = damageRecords.Count - 1; i >= 0; i--)
        {
            var record = damageRecords[i];
            accumulatedDamage += record.damage;

            if (record.attackerNetId != killerNetId &&
                assistBuffer.Add(record.attackerNetId))
            {
                if (NetworkServer.spawned.TryGetValue(record.attackerNetId, out var identity))
                {
                    var assister = identity.GetComponent<Stat>();
                    if (assister != null && IsEnemy(assister))
                        assister.AddAssist();
                }
            }

            if (accumulatedDamage >= MaxHP)
                break;
        }
    }

    private void Revive()
    {
        IsDead = false;
        InitHP();
    }

    private IEnumerator<float> Respawn()
    {
        yield return Timing.WaitForSeconds(roomManager.RespawnDelay);
        Revive();
    }

    [Server]
    public void SetTeam(Team newTeam)
    {
        MyTeam = newTeam;
    }

    #region KDA
    [Server]
    public void AddKill()
    {
        var kda = CurrentKDA;
        kda.Kills++;
        CurrentKDA = kda;
    }

    [Server]
    public void AddAssist()
    {
        var kda = CurrentKDA;
        kda.Assists++;
        CurrentKDA = kda;
    }

    [Server]
    public void AddDeath()
    {
        var kda = CurrentKDA;
        kda.Deaths++;
        CurrentKDA = kda;
    }

    private bool IsEnemy(Stat other)
    {
        return other != null && other.MyTeam != MyTeam;
    }
    #endregion

    private void OnTeamChanged(Team oldValue, Team newTeam)
    {
        gameObject.layer = newTeam == Team.Blue
            ? LayerMask.NameToLayer("TeamBlue")
            : LayerMask.NameToLayer("TeamRed");

        gameObject.tag = newTeam == Team.Blue
            ? "TeamBlue"
            : "TeamRed";

        if (!isLocalPlayer) marker.SetColor(newTeam);

        var meshUpdater = GetComponent<CharacterMeshUpdater>();
        if (meshUpdater != null)
        {
            string meshID = newTeam == Team.Blue ? "Blue" : "Red";
            meshUpdater.UpdateCharacterMesh(meshID);
        }
        else
        {
            Debug.LogWarning($"CharacterMeshUpdater not ready on {gameObject.name}");
        }
    }

    private void OnNicknameChanged(string oldValue, string newValue)
    {
        if (!isLocalPlayer) marker.SetNickname(newValue);
    }

    private void OnHPChanged(float oldHp, float newHp)
    {
        if (isLocalPlayer)
        {
            HealthGuageHUD.Instance.UpdateHP(newHp, MaxHP);
        }
    }

    private void OnDeathStateChanged(bool oldState, bool newState)
    {
        if (newState)
        {
            OnDead?.Invoke();
            gameObject.SetActive(false);
            LastDeadPosition = transform.position;
        }
        else
        {
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;

            gameObject.SetActive(true);
            OnRevive?.Invoke();
        }
    }

    private void OnKDAChanged(KDA oldValue, KDA newValue)
    {
        if (!isClient) return;

        ScoreboardHUD.Instance?.UpdateKDA(
            netId,
            newValue.Kills,
            newValue.Assists,
            newValue.Deaths
        );
    }
}
