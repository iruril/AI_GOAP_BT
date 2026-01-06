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

public class Stat : NetworkBehaviour, IDamageable
{
    public event Action OnDead;
    public event Action OnRevive;
    public event Action<Vector3> OnUnderAttack;

    [SyncVar(hook = nameof(OnTeamChanged))]
    public Team MyTeam = Team.Blue;

    [SyncVar]
    public string Nickname;

    [SerializeField] private float maxHP = 100f;
    public float MaxHP => maxHP;
    [SerializeField] private float rotateSpeedToTarget = 90f;
    public float RotateSpeedToTarget => rotateSpeedToTarget;

    [SyncVar(hook = nameof(OnHPChanged))] public float CurrentHP;
    [SyncVar(hook = nameof(OnDeathStateChanged))] public bool IsDead = false;

    [SyncVar(hook = nameof(OnKDAChanged))]
    public KDA CurrentKDA = new();

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    public CapturePoint.CapturePoint CurrentCapture { get; set; } = null;

    private float lastDamageTime = -999f;
    private CoroutineHandle hpRegenHandle;

    private HashSet<uint> damageContributors = new();
    public uint KillerNetId { get; private set; }

    bool combatEnded = false;
    private const float NO_DAMAGE_DURATION = 5f;
    private const float REGEN_RATE = 0.1f;

    public override void OnStartServer()
    {
        InitHP();
        hpRegenHandle = Timing.RunCoroutine(HPRegenHandle());

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
    }

    public override void OnStopServer()
    {
        Timing.KillCoroutines(hpRegenHandle);
    }

    private void InitHP()
    {
        CurrentHP = MaxHP;
    }

    #region Damageable Field
    [Server]
    public virtual void ApplyDamage(float dmg, Vector3 shotOrigin, Vector3 hitPoint)
    {
        if (IsDead) return;

        combatEnded = false;
        CurrentHP -= dmg;
        lastDamageTime = Time.time;

        OnUnderAttack?.Invoke(shotOrigin);

        if (CurrentHP <= 0f)
        {
            Die();
            Timing.RunCoroutine(Respawn());
        }
    }

    [Server]
    public void OnGraze(Vector3 shotOrigin)
    {
        OnUnderAttack?.Invoke(shotOrigin);
    }

    [Server]
    public void SetKiller(uint attackerNetId)
    {
        KillerNetId = attackerNetId;
    }

    [Server]
    public void AddDmgContributer(uint attackerNetId)
    {
        damageContributors.Add(attackerNetId);
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
                if (!combatEnded)
                {
                    damageContributors.Clear();
                    combatEnded = true;
                }

                float regenAmount = MaxHP * REGEN_RATE * 0.1f;
                CurrentHP = Mathf.Min(CurrentHP + regenAmount, MaxHP);
            }
        }
    }

    private void Die()
    {
        CurrentHP = 0f;
        IsDead = true;

        AddDeath();

        // Killer 처리
        if (NetworkServer.spawned.TryGetValue(KillerNetId, out var killerIdentity))
        {
            var killerStat = killerIdentity.GetComponent<Stat>();
            if(KillerNetId != netId) killerStat?.AddKill();

            LogManager.Instance.ReportKill(
                killerStat.Nickname,
                Nickname,
                killerStat.MyTeam == Team.Blue,
                MyTeam == Team.Blue
            );

            GameFlowManager.Instance.ApplyKillScore(
                killerStat.MyTeam,
                MyTeam
            );
        }

        // Assist 처리
        foreach (uint contributorNetId in damageContributors)
        {
            if (contributorNetId == KillerNetId)
                continue; // 킬러 제외

            if (NetworkServer.spawned.TryGetValue(contributorNetId, out var identity))
            {
                var assister = identity.GetComponent<Stat>();
                if (contributorNetId != netId) assister?.AddAssist();
            }
        }

        damageContributors.Clear();
        CurrentCapture?.RemoveIntruder(this);
    }

    private void Revive()
    {
        IsDead = false;
        InitHP();
        damageContributors.Clear();
    }

    private IEnumerator<float> Respawn()
    {
        yield return Timing.WaitForSeconds(GameManager.GetInstance().RespawnTime);
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
    #endregion

    private void OnTeamChanged(Team oldValue, Team newTeam)
    {
        gameObject.layer = newTeam == Team.Blue
            ? LayerMask.NameToLayer("TeamBlue")
            : LayerMask.NameToLayer("TeamRed");

        gameObject.tag = newTeam == Team.Blue
            ? "TeamBlue"
            : "TeamRed";

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
