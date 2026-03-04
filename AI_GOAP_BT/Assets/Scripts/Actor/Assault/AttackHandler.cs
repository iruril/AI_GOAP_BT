using MEC;
using Mirror;
using RootMotion.FinalIK;
using System.Collections.Generic;
using UnityEngine;

namespace GOAP.Assualt
{
    public class AttackHandler : NetworkBehaviour
    {
        AssaultBrain myBrain;

        [Header("Shoot Interval")]
        [SerializeField] private float attackCooldown = 2.5f;
        private int burstCount = 3; 
        
        [Header("Reaction Settings")]
        [SerializeField] private float reactionTime = 0.5f;
        private float recognitionTimer = 0f;

        [Header("Accuracy Settings")]
        [SerializeField] private float baseSpread = 0.5f;
        [SerializeField] private float spreadPerShot = 0.2f;
        [SerializeField] private float maxSpread = 1.5f;
        [SerializeField] private float aimTrackingLag = 0.1f;

        private float currentSpread = 0f;
        private Vector3 visualAimOffset;

        private float cooldownTimer = 0f;
        private bool isBursting = false;
        private CoroutineHandle burstHandle;

        [Header("SyncVar")]
        [SyncVar] public Vector3 SyncedAimTarget;
        [SyncVar] public float SyncedAimWeight;

        private void Awake()
        {
            myBrain = GetComponent<AssaultBrain>();
        }

        private void OnDisable()
        {
            if (isServer) SyncedAimWeight = 0f;
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
            myBrain.MotionController.FBBIK.solver.GetBendConstraint(FullBodyBipedChain.LeftArm).bendGoal = myBrain.GunController.LeftArmIKHint;
            myBrain.MotionController.FBBIK.solver.GetBendConstraint(FullBodyBipedChain.LeftArm).weight = 1f;
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
                
                if (myBrain.Sensor.TargetVisible && myBrain.Sensor.HasTarget)
                {
                    recognitionTimer += Time.deltaTime;
                }
                else
                {
                    recognitionTimer = 0f;
                }
            }

            ClientUpdateIK();
        }

        private Vector3 serverAimVel;
        float serverAimWeightVel;
        private void ServerUpdateAimValues()
        {
            bool hasValidLastSeen = myBrain.Sensor.LastSeenPosition != Vector3.negativeInfinity;

            Vector3 realTargetPos = (myBrain.Sensor.IsAlert && hasValidLastSeen)
                ? myBrain.Sensor.LastSeenPosition
                : transform.position + transform.forward * 20f + Vector3.up * 1.2f;

            if (float.IsNaN(realTargetPos.x) || float.IsInfinity(realTargetPos.x))
            {
                realTargetPos = transform.position + transform.forward * 5f;
            }

            if (float.IsNaN(SyncedAimTarget.x) || float.IsInfinity(SyncedAimTarget.x))
            {
                SyncedAimTarget = realTargetPos;
                serverAimVel = Vector3.zero;
            }

            float safeLag = Mathf.Max(0.01f, aimTrackingLag);
            SyncedAimTarget = Vector3.SmoothDamp(
                    SyncedAimTarget,
                    realTargetPos + visualAimOffset,
                    ref serverAimVel,
                    safeLag
                );

            if (float.IsNaN(SyncedAimWeight)) SyncedAimWeight = 0f;

            float targetWeight = (myBrain.MotionController.IsAimable && myBrain.Sensor.IsAlert && !myBrain.GunController.OnReload) ? 1f : 0f;

            SyncedAimWeight = Mathf.SmoothDamp(
                    SyncedAimWeight,
                    targetWeight,
                    ref serverAimWeightVel,
                    Mathf.Max(0.01f, myBrain.GunController.CurrentGun.GunInfo.TimeToADS)
                );
        }

        private void ClientUpdateIK()
        {
            bool isPosValid = !float.IsInfinity(SyncedAimTarget.x) && !float.IsNaN(SyncedAimTarget.x);
            if (isPosValid)
            {
                myBrain.GunController.AimIKTarget.position = SyncedAimTarget;
            }

            float targetWeight = isPosValid ? SyncedAimWeight : 0f;
            myBrain.MotionController.AimIK.solver.IKPositionWeight = targetWeight;
        }

        public void TryAttack()
        {
            if (!isServer) return;
            if (!myBrain.MotionController.Shootable()) return;
            if (cooldownTimer < attackCooldown) return;
            if (isBursting) return; 
            if (recognitionTimer < reactionTime) return;

            burstHandle = Timing.RunCoroutine(BurstRoutine());
        }

        private IEnumerator<float> BurstRoutine()
        {
            isBursting = true;
            cooldownTimer = 0f;
            currentSpread = baseSpread;

            var gunStat = myBrain.GunController;
            int targetShots = burstCount;
            int lastAmmo = gunStat.CurrentRounds;

            float timeSinceLastShot = 0f;
            bool isTriggerPressed = true;

            UpdateVisualAimOffset();

            while (targetShots > 0)
            {
                if (!myBrain.MotionController.Shootable() || gunStat.CurrentRounds <= 0) break;

                myBrain.GunController.TryFire(isPressed: isTriggerPressed, isHeld: true);
                isTriggerPressed = false;

                if (gunStat.CurrentRounds < lastAmmo)
                {
                    int shotsFiredNow = lastAmmo - gunStat.CurrentRounds;
                    targetShots -= shotsFiredNow;
                    lastAmmo = gunStat.CurrentRounds;

                    currentSpread = Mathf.Min(currentSpread + spreadPerShot, maxSpread);
                    timeSinceLastShot = 0f;

                    UpdateVisualAimOffset();
                }
                else
                {
                    timeSinceLastShot += Time.deltaTime;
                }

                float gracePeriod = gunStat.CurrentGun.GunInfo.ShotInterval + 0.05f;
                if (timeSinceLastShot > gracePeriod)
                {
                    isTriggerPressed = true;
                }

                yield return Timing.WaitForOneFrame;
            }

            visualAimOffset = Vector3.zero;
            isBursting = false;
            currentSpread = baseSpread;
        }

        private void UpdateVisualAimOffset()
        {
            Vector3 realTargetPos = myBrain.Sensor.LastSeenPosition;
            float distance = Vector3.Distance(transform.position, realTargetPos);
            float distanceMultiplier = distance / 10f;
            visualAimOffset = Random.insideUnitSphere * (currentSpread * distanceMultiplier);
        }

        private void OnDead()
        {
            cooldownTimer = 0f;
            visualAimOffset = Vector3.zero;
            isBursting = false;
            currentSpread = baseSpread;
            Timing.KillCoroutines(burstHandle);
        }

        private void OnGunChanged()
        {
            // 다 맞아도 죽지 않을 정도로만 격발
            int rounds = Mathf.CeilToInt(myBrain.Sensor.MyStat.MaxHP / (float)myBrain.GunController.CurrentGun.GunInfo.RoundDamage);
            burstCount = Mathf.Max(1, rounds - 1);
        }
    }
}
