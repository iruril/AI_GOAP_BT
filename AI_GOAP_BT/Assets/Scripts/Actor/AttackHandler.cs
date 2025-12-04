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
    }

    private void OnDestroy()
    {
        myBrain.Sensor.MyStat.OnDead -= OnDead;
    }

    private void Update()
    {
        cooldownTimer += Time.deltaTime;
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
