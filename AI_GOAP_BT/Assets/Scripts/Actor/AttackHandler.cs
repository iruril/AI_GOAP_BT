using UnityEngine;
using MEC;
using System.Collections.Generic;

public class AttackHandler : MonoBehaviour
{
    GOAP.Assualt.AssaultBrain myBrain;

    [Header("공격 주기")]
    [SerializeField] private float attackCooldown = 2.5f;

    [Header("점사 탄환 수")]
    [SerializeField] private int burstCount = 3;

    private float cooldownTimer = 0f;
    private bool isBursting = false;
    private CoroutineHandle burstHandle;

    private void Awake()
    {
        myBrain = GetComponent<GOAP.Assualt.AssaultBrain>();
    }

    private void Start()
    {
        myBrain.Sensor.MyStat.OnDead += OnDead;
        myBrain.Sensor.MyStat.OnDead += myBrain.GunController.OnDead;
        myBrain.MotionController.AimIK.solver.OnPostUpdate += myBrain.GunController.FireCallback;
        myBrain.MotionController.FBBIK.solver.leftHandEffector.target = myBrain.GunController.LeftHandIKTarget;
        myBrain.MotionController.FBBIK.solver.leftHandEffector.positionWeight = 1f;
        myBrain.MotionController.AimIK.solver.IKPositionWeight = 0f;
    }

    private void OnDestroy()
    {
        myBrain.Sensor.MyStat.OnDead -= OnDead;
        myBrain.Sensor.MyStat.OnDead -= myBrain.GunController.OnDead;
        myBrain.MotionController.AimIK.solver.OnPostUpdate -= myBrain.GunController.FireCallback;
    }

    private void Update()
    {
        cooldownTimer += Time.deltaTime;
        AimIKHandle();
    }

    private void AimIKHandle()
    {
        AimIKTargetTransformControl();
        AimIKWeightControl();
    }

    Vector3 aimIkTargetPosRef;
    private void AimIKTargetTransformControl()
    {
        Vector3 targetPos = myBrain.Sensor.IsAlert
            ? myBrain.Sensor.LastSeenPosition
            : transform.position + transform.forward * 20f + Vector3.up * 1.2f;

        myBrain.GunController.AimIKTarget.position = Vector3.SmoothDamp
        (
            myBrain.GunController.AimIKTarget.position,
            targetPos,
            ref aimIkTargetPosRef,
            0.25f,
            float.PositiveInfinity,
            Time.deltaTime
        );
    }


    float _refTargetValue;
    private void AimIKWeightControl()
    {
        float _targetVaule = myBrain.MotionController.Aimable() && !myBrain.GunController.OnReload ? 1f : 0f;

        myBrain.MotionController.AimIK.solver.IKPositionWeight = Mathf.SmoothDamp(
            myBrain.MotionController.AimIK.solver.IKPositionWeight,
            _targetVaule,
            ref _refTargetValue,
            0.1f
        );
    }

    public void TryAttack()
    {
        if (!myBrain.MotionController.Shootable())
            return;

        if (cooldownTimer < attackCooldown)
            return;

        if (isBursting)
            return;

        burstHandle = Timing.RunCoroutine(BurstRoutine());
    }

    private IEnumerator<float> BurstRoutine()
    {
        isBursting = true;
        cooldownTimer = 0f;

        var gunStat = myBrain.GunController;
        int fireCount = burstCount;

        while (fireCount > 0)
        {
            if (!myBrain.MotionController.Shootable()) break;
            if (gunStat.CurrentRounds <= 0) break;

            myBrain.GunController.Fire();
            fireCount--;

            yield return Timing.WaitForSeconds(myBrain.GunController.CurrentGun.GunInfo.ShotInterval);
        }

        isBursting = false;
    }

    private void OnDead()
    {
        cooldownTimer = 0f;
        isBursting = false;
        Timing.KillCoroutines(burstHandle);
    }
}
