using UnityEngine;
using System;
using System.Collections.Generic;
using MEC;

public class Stat : MonoBehaviour, IDamageable
{
    public event Action OnDead;
    public event Action OnRevive;
    public event Action<Vector3> OnUnderAttack;

    [SerializeField] private float maxHP = 100f;
    public float MaxHP => maxHP;
    [SerializeField] private float rotateSpeedToTarget = 90f;
    public float RotateSpeedToTarget => rotateSpeedToTarget;

    public float CurrentHP { get; private set; }
    public bool IsDead { get; private set; } = false;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    public CapturePoint.CapturePoint CurrentCapture { get; set; } = null;

    private float lastDamageTime = -999f;
    private CoroutineHandle hpRegenHandle;

    private const float NO_DAMAGE_DURATION = 5f;
    private const float REGEN_RATE = 0.1f;

    private void Awake()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        InitHP();
    }

    private void Start()
    {
        OnDead += ReleaseCapturePoint;
        OnRevive += Revive;

        hpRegenHandle = Timing.RunCoroutine(HPRegenHandle());
    }

    private void OnDestroy()
    {
        OnDead -= ReleaseCapturePoint;
        OnRevive -= Revive;

        Timing.KillCoroutines(hpRegenHandle);
    }

    private void InitHP()
    {
        CurrentHP = MaxHP;
    }

    #region Damageable Field
    public virtual void ApplyDamage(float dmg, Vector3 shotOrigin, Vector3 hitPoint)
    {
        if (IsDead) return;

        CurrentHP -= dmg;
        lastDamageTime = Time.time;

        OnUnderAttack?.Invoke(shotOrigin);

        if (CurrentHP <= 0f)
        {
            CurrentHP = 0f;
            IsDead = true;

            OnDead?.Invoke();
            Timing.RunCoroutine(Respawn());
        }
    }

    public void OnGraze(Vector3 shotOrigin)
    {
        OnUnderAttack?.Invoke(shotOrigin);
    }

    #endregion

    private void ReleaseCapturePoint()
    {
        CurrentCapture?.RemoveIntruder(this);
    }

    private void Revive()
    {
        InitHP();
        IsDead = false;
    }

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

    private IEnumerator<float> Respawn()
    {
        gameObject.SetActive(false);
        yield return Timing.WaitForSeconds(GameManager.GetInstance().RespawnTime);

        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
        gameObject.SetActive(true);

        OnRevive?.Invoke();
    }
}
