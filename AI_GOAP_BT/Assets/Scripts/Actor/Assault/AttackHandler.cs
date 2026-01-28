using UnityEngine;
using MEC;
using System.Collections.Generic;
using Mirror;

namespace GOAP.Assualt
{
    public class AttackHandler : NetworkBehaviour
    {
        AssaultBrain myBrain;

        [Header("공격 주기")]
        [SerializeField] private float attackCooldown = 2.5f;
        private int burstCount = 3;

        private float cooldownTimer = 0f;
        private bool isBursting = false;
        private CoroutineHandle burstHandle;

        [SyncVar] private Vector3 syncedAimTarget;
        [SyncVar] private float syncedAimWeight;

        private void Awake()
        {
            myBrain = GetComponent<AssaultBrain>();
        }

        private void OnDisable()
        {
            syncedAimWeight = 0f;
            myBrain.MotionController.AimIK.solver.IKPositionWeight = 0f;
        }

        public override void OnStartServer()
        {
            myBrain.Sensor.MyStat.OnDead += OnDead;
            myBrain.Sensor.MyStat.OnDead += myBrain.GunController.OnDead;
            myBrain.MotionController.AimIK.solver.OnPostUpdate += myBrain.GunController.FireCallback;

            Timing.RunCoroutine(WaitForGunInitialization());
        }

        private IEnumerator<float> WaitForGunInitialization()
        {
            while (myBrain.GunController.CurrentGun == null)
            {
                yield return Timing.WaitForOneFrame;
            }

            OnGunChanged();
        }

        public override void OnStartClient()
        {
            myBrain.MotionController.AimIK.solver.IKPositionWeight = 0f;
            myBrain.MotionController.FBBIK.solver.leftHandEffector.target = myBrain.GunController.LeftHandIKTarget;
            myBrain.MotionController.FBBIK.solver.leftHandEffector.positionWeight = 1f;
        }

        public override void OnStopServer()
        {
            myBrain.Sensor.MyStat.OnDead -= OnDead;
            myBrain.Sensor.MyStat.OnDead -= myBrain.GunController.OnDead;
            myBrain.MotionController.AimIK.solver.OnPostUpdate -= myBrain.GunController.FireCallback;
            Timing.KillCoroutines(burstHandle);
        }

        private void Update()
        {
            if (isServer)
            {
                cooldownTimer += Time.deltaTime;
                ServerUpdateAimValues();
            }

            ClientUpdateIK();
        }

        private void ServerUpdateAimValues()
        {
            syncedAimTarget = myBrain.Sensor.IsAlert && myBrain.Sensor.LastSeenPosition != Vector3.negativeInfinity
                ? myBrain.Sensor.LastSeenPosition
                : transform.position + transform.forward * 20f + Vector3.up * 1.2f;

            syncedAimWeight = (myBrain.MotionController.IsAimable &&
                               !myBrain.GunController.OnReload)
                               ? 1f : 0f;
        }

        Vector3 aimPosVel;
        float aimWeightVel;
        private void ClientUpdateIK()
        {
            bool isPosValid = !float.IsInfinity(syncedAimTarget.x) && !float.IsNaN(syncedAimTarget.x);
            if (isPosValid)
            {
                myBrain.GunController.AimIKTarget.position =
                Vector3.SmoothDamp(
                    myBrain.GunController.AimIKTarget.position,
                    syncedAimTarget,
                    ref aimPosVel,
                    0.25f,
                    Mathf.Infinity,
                    Time.deltaTime
                );
            }
            else
            {
                aimPosVel = Vector3.zero;
            }

            float targetWeight = isPosValid ? syncedAimWeight : 0f;

            myBrain.MotionController.AimIK.solver.IKPositionWeight =
                Mathf.SmoothDamp(
                    myBrain.MotionController.AimIK.solver.IKPositionWeight,
                    targetWeight,
                    ref aimWeightVel,
                    0.1f
                );
        }

        public void TryAttack()
        {
            if (!isServer) return;
            if (!myBrain.MotionController.Shootable()) return;
            if (cooldownTimer < attackCooldown) return;
            if (isBursting) return;

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

        private void OnGunChanged()
        {
            // 다 맞아도 죽지 않을 정도로만 격발
            burstCount = Mathf.CeilToInt(myBrain.Sensor.MyStat.MaxHP /
                                (float)myBrain.GunController.CurrentGun.GunInfo.RoundDamage) - 1;
        }
    }
}
