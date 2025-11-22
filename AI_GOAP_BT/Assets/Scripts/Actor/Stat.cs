using UnityEngine;
using System;
using System.Collections.Generic;
using MEC;
using AnimControl.Assault;

public class Stat : MonoBehaviour, IDamageable
{
    public event Action OnDead;
    public event Action OnRevive;

    [SerializeField] private float MaxHP = 100f;
    [SerializeField] private float rotateSpeedToTarget = 90f;
    public float RotateSpeedToTarget => rotateSpeedToTarget;

    public float CurrentHP { get; private set; }
    public bool IsDead { get; private set; } = false;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    public CapturePoint.CapturePoint CurrentCapture { get; set; } = null;

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
    }

    private void OnDestroy()
    {
        OnDead -= ReleaseCapturePoint;
        OnRevive -= Revive;
    }

    private void InitHP()
    {
        CurrentHP = MaxHP;
    }

    #region Damageable Field
    public virtual void ApplyDamage(float dmg, Vector3 shotOriginm, Vector3 hitPoint)
    {
        if (IsDead) return;

        CurrentHP -= dmg;

        Vector3 hitDir = shotOriginm - transform.position;
        hitDir.y = 0f;
        GetComponent<AssaultAnimFSM>()?.OnHit(hitDir.normalized);

        if (CurrentHP <= 0f)
        {
            CurrentHP = 0f;
            IsDead = true;

            OnDead?.Invoke();
            Timing.RunCoroutine(Respawn());
        }
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

    private IEnumerator<float> Respawn()
    {
        gameObject.SetActive(false);
        yield return Timing.WaitForSeconds(10f);

        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
        gameObject.SetActive(true);

        OnRevive?.Invoke();
    }
}
