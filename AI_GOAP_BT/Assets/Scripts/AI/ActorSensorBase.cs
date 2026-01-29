using MEC;
using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace Sensor
{

    public abstract class ActorSensorBase : NetworkBehaviour
    {
        static readonly HumanBodyBones[] AimBones =
        {
            HumanBodyBones.UpperChest,
            HumanBodyBones.Chest,
            HumanBodyBones.Hips,
            HumanBodyBones.LeftUpperLeg,
            HumanBodyBones.RightUpperLeg,
            HumanBodyBones.LeftUpperArm,
            HumanBodyBones.RightUpperArm,
            HumanBodyBones.Head,
        };

        public Stat MyStat { get; private set; }

        [Header("My Eyes")]
        [SerializeField] private Transform myEyes;
        public Transform MyEyes => myEyes;

        [Header("Target Info")]
        public Transform CurrentTarget { get; private set; }
        public Animator TargetAnimatorBones { get; private set; }
        public Transform CurrentTargetAimPoint { get; private set; }
        public Stat CurrentTargetStat { get; private set; }
        public bool HasTarget => CurrentTarget != null;

        public bool TargetVisible { get; private set; } = false;

        [Header("Target Memory")]
        public Vector3 LastSeenPosition { get; private set; } = Vector3.negativeInfinity;
        [SerializeField] private float alertDuration = 20f;
        private float alertTimer;
        public bool IsAlert { get; private set; } = false;

        [Header("Capture Info")]
        public CapturePoint.CapturePoint CaptureTarget { get; private set; }
        [SerializeField] private float captureOffsetRadius = 4f;

        [Header("Sight Info")]
        [SerializeField] private float sightCheckInterval = 0.15f;
        [SerializeField] private float sightRange = 50f;
        [SerializeField] private float sightAngle = 160f;
        private float cosHalfFov;
        private Collider[] sightBuffer = new Collider[8]; 
        private RaycastHit[] rayHits = new RaycastHit[1];

        [Header("Tactical Settings")]
        [SerializeField] private float targetLoyaltyBonus = 2.0f; //기존 타겟에게 주는 가산점
        [SerializeField] private float searchBetterTargetInterval = 0.5f;
        private float lastSearchTime = 0f;

        protected CoroutineHandle underAttackHandle;
        protected CoroutineHandle sightCheckHandle;

        protected virtual void Awake()
        {
            MyStat = GetComponent<Stat>();
            cosHalfFov = Mathf.Cos((sightAngle * 0.5f) * Mathf.Deg2Rad);
        }

        public override void OnStartServer()
        {
            MyStat.OnDead += OnDead;
            MyStat.OnRevive += OnRevive;
            sightCheckHandle = Timing.RunCoroutine(CheckSightRoutine());
        }

        public override void OnStopServer()
        {
            MyStat.OnDead -= OnDead;
            MyStat.OnRevive -= OnRevive;
            Timing.KillCoroutines(sightCheckHandle);
            Timing.KillCoroutines(underAttackHandle);
        }

        protected virtual void Update()
        {
            if (!isServer) return;
            UpdateAlertTimer();
            UpdateLastSeenPosition();
        }

        protected virtual void FixedUpdate()
        {
            if (!isServer) return;
        }

        private void UpdateAlertTimer()
        {
            if (!IsAlert) return;

            if (TargetVisible)
            {
                alertTimer = 0f;
            }
            else
            {
                alertTimer += Time.deltaTime;

                if (alertTimer >= alertDuration)
                {
                    IsAlert = false;
                    ResetTarget();
                }
            }
        }

        private void UpdateLastSeenPosition()
        {
            if (!TargetVisible) return;

            LastSeenPosition = CurrentTargetAimPoint.position;
        }

        protected virtual void SetTarget(Transform target)
        {
            CurrentTarget = target;

            if (target.TryGetComponent<Stat>(out var stat)) 
                CurrentTargetStat = stat;

            if (target.TryGetComponent<Animator>(out var anim))
                TargetAnimatorBones = anim;

            CurrentTargetAimPoint = target;
            IsAlert = true;
        }

        protected virtual void ResetTarget()
        {
            CurrentTarget = null;
            CurrentTargetStat = null;
            CurrentTargetAimPoint = null;
            TargetVisible = false;
        }

        #region Capture Field
        public void ResetCapture()
        {
            CaptureTarget = null;
        }

        public void GetClosestCapture(out Vector3 destination)
        {
            CaptureTarget = WorldManager.Instance.RequestClosestCapture(transform, captureOffsetRadius, out destination);
        }

        public bool IsCurrentCapCapturerd()
        {
            return !CaptureTarget.NeedToCapture(transform);
        }
        #endregion

        #region Sight Check & Assgin Target Field
        protected void CheckHostileInSight()
        {
            if (HasTarget && TargetVisible && Time.time < lastSearchTime + searchBetterTargetInterval)
                return;

            lastSearchTime = Time.time;

            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                sightRange,
                sightBuffer,
                WorldManager.Instance.IsBlueTeam(gameObject.layer) 
                ? WorldManager.Instance.GetRedTeamLayers() 
                : WorldManager.Instance.GetBlueTeamLayers()
            );

            if (hitCount == 0)
            {
                ResetTarget();
                return;
            }

            Transform best = SelectBestVisibleTarget(hitCount);

            if (best != null && best != CurrentTarget)
            {
                SetTarget(best);
            }
        }

        private Transform SelectBestVisibleTarget(int hitCount)
        {
            Transform best = null;
            Transform bestAimPoint = null;
            float bestScore = float.MinValue;

            for (int i = 0; i < hitCount; i++)
            {
                Transform candidate = sightBuffer[i]?.transform;
                if (candidate == null) continue;

                if (!IsInSightAngle(candidate, myEyes.position)) continue;
                if (!candidate.TryGetComponent<Animator>(out var anim)) continue;
                if (!TryFindVisibleAimPoint(anim, out var aimPoint)) continue;

                float score = CalculateTargetScore(candidate, myEyes.position);

                if(candidate == CurrentTarget)
                {
                    score += targetLoyaltyBonus;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                    bestAimPoint = aimPoint;
                }
            }

            if(best != null)
            {
                CurrentTargetAimPoint = bestAimPoint;
            }

            return best;
        }

        private float CalculateTargetScore(Transform target, Vector3 origin)
        {
            Vector3 fullDir = target.position - origin;
            float sqrDist = fullDir.sqrMagnitude;

            float distScore = 1000f / (10f + sqrDist);

            Vector3 dirNormalized = fullDir.normalized;
            float dot = Vector3.Dot(transform.forward, dirNormalized);
            float angleScore = dot * 5f;

            return distScore + angleScore;
        }

        private IEnumerator<float> CheckSightRoutine()
        {
            while (true)
            {
                CheckHostileInSight();
                CheckTargetInSight(); 
                CheckTargetIsValid();

                yield return Timing.WaitForSeconds(sightCheckInterval);
            }
        }

        protected void CheckTargetInSight()
        {
            if (!HasTarget || TargetAnimatorBones == null)
            {
                TargetVisible = false;
                return;
            }

            if (TryFindVisibleAimPoint(TargetAnimatorBones, out var aimPoint))
            {
                TargetVisible = true;
                CurrentTargetAimPoint = aimPoint;
            }
            else
            {
                TargetVisible = false;
            }
        }

        protected void CheckTargetIsValid()
        {
            if (!HasTarget || CurrentTargetStat == null) return;

            if (CurrentTargetStat.IsDead)
            {
                ResetTarget();
                return;
            }
            
            float sqrSightRange = sightRange * sightRange; 
            Vector3 fullDir = CurrentTarget.position - myEyes.position;

            if (fullDir.sqrMagnitude > sqrSightRange * 1.44f || !IsInSightAngle(CurrentTarget, transform.position))
            {
                TargetVisible = false;
                return;
            }
        }

        private bool IsInSightAngle(Transform target, Vector3 origin)
        {
            Vector3 dir = target.position - origin;

            float sqrDist = dir.sqrMagnitude;
            if (sqrDist > sightRange * sightRange)
                return false;

            dir.Normalize();

            return Vector3.Dot(transform.forward, dir) >= cosHalfFov;
        }

        private bool TryFindVisibleAimPoint(Animator targetAnim, out Transform visiblePoint)
        {
            visiblePoint = null; 
            bool isBlue = WorldManager.Instance.IsBlueTeam(gameObject.layer);
            LayerMask obstacleMask = WorldManager.Instance.GetLevelLayers();
            LayerMask myTeamMask = isBlue ? WorldManager.Instance.GetBlueTeamLayers() : WorldManager.Instance.GetRedTeamLayers();
            
            LayerMask combinedMask = obstacleMask | myTeamMask;

            foreach (var bone in AimBones)
            {
                Transform t = targetAnim.GetBoneTransform(bone);
                if (t == null) continue;

                Vector3 dir = t.position - myEyes.position;
                float dist = dir.magnitude;
                if (dist <= 0.01f) continue;

                dir /= dist;

                int hitCount = Physics.SphereCastNonAlloc(myEyes.position, 0.05f, dir, rayHits, dist, combinedMask);
                
                bool isBlocked = false;
                for (int i = 0; i < hitCount; i++)
                {
                    if (rayHits[i].transform != transform && rayHits[i].transform != targetAnim.transform)
                    {
                        if (rayHits[i].distance < dist - 0.1f)
                        {
                            isBlocked = true;
                            break;
                        }
                    }
                }

                if (!isBlocked)
                {
                    visiblePoint = t;
                    return true;
                }
            }

            return false;
        }
        #endregion

        private void OnRevive()
        {
            sightCheckHandle = Timing.RunCoroutine(CheckSightRoutine());
        }

        private void OnDead()
        {
            ResetTarget();
            ResetCapture();
            Timing.KillCoroutines(underAttackHandle);
            Timing.KillCoroutines(sightCheckHandle);
            IsAlert = false;
            alertTimer = 0f;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (HasTarget)
            {
                if (transform.CompareTag("TeamBlue"))
                {
                    Gizmos.color = Color.cyan;

                    Vector3 originEye = myEyes.position;
                    Vector3 targetEye = LastSeenPosition;

                    Gizmos.DrawLine(originEye, targetEye);

                }
                else
                {
                    Gizmos.color = Color.magenta;

                    Vector3 originEye = myEyes.position - Vector3.up * 0.05f;
                    Vector3 targetEye = LastSeenPosition - Vector3.up * 0.05f;

                    Gizmos.DrawLine(originEye, targetEye);
                }
            }
        }
#endif
    }
}
