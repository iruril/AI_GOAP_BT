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

        if (isServer)
            InitHP();
    }

    private void Start()
    {
        if (!isServer) return;
        hpRegenHandle = Timing.RunCoroutine(HPRegenHandle());
    }

    private void OnDestroy()
    {
        if (!isServer) return;
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
            WorldManager.Instance.IsBlueTeam(1 << gameObject.layer)
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

    private void OnHPChanged(float oldHp, float newHp)
    {
        // 필요하면 HP바 UI 갱신 등 클라이언트 처리
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
