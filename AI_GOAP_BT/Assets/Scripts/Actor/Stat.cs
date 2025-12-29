using UnityEngine;
using System;
using System.Collections.Generic;
using MEC;
using Mirror;

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

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    public CapturePoint.CapturePoint CurrentCapture { get; set; } = null;

    private float lastDamageTime = -999f;
    private CoroutineHandle hpRegenHandle;

    public string KillerNickname { get; set; }
    public bool IsKillerBlue { get; set; }

    private const float NO_DAMAGE_DURATION = 5f;
    private const float REGEN_RATE = 0.1f;

    private void Awake()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
    }

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
    public virtual void ApplyDamage(float dmg, Vector3 shotOrigin, Vector3 hitPoint)
    {
        if (!isServer) return;
        if (IsDead) return;

        CurrentHP -= dmg;
        lastDamageTime = Time.time;

        OnUnderAttack?.Invoke(shotOrigin);

        if (CurrentHP <= 0f)
        {
            Die();
            Timing.RunCoroutine(Respawn());
        }
    }

    public void OnGraze(Vector3 shotOrigin)
    {
        if (!isServer) return;
        OnUnderAttack?.Invoke(shotOrigin);
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
        }
    }

    private void Die()
    {
        CurrentHP = 0f;
        IsDead = true;
        LogManager.Instance.ReportKill(
            KillerNickname,
            Nickname,
            IsKillerBlue,
            WorldManager.Instance.IsBlueTeam(this.gameObject.layer)
        );
        CurrentCapture?.RemoveIntruder(this);
    }

    private void Revive()
    {
        IsDead = false;
        InitHP();
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
            HealthGuageHUD.Instance.SetHealth(newHp, MaxHP);
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
}
