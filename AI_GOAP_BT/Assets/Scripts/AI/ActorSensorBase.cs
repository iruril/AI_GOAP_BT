using MEC;
using System.Collections.Generic;
using UnityEngine;

namespace Sensor
{
    public abstract class ActorSensorBase : MonoBehaviour
    {
        public Stat MyStat { get; private set; }

        [Header("My Eyes")]
        [SerializeField] private Transform myEyes;
        public Transform MyEyes => myEyes;

        [Header("Target Info")]
        public Transform CurrentTarget { get; private set; }
        public Transform CurrentTargetHead { get; private set; }
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
        [SerializeField] private float sightRange = 50f;
        [SerializeField] private float sightAngle = 160f;
        private float cosHalfFov;
        [SerializeField] private float visibleOffesetHight = 1.4f;
        private Collider[] sightBuffer = new Collider[8]; 
        private RaycastHit[] rayHits = new RaycastHit[1];

        [Header("Combat Info")]
        [SerializeField] private LayerMask enemyLayer;

        protected CoroutineHandle underAttackHandle;

        protected virtual void Awake()
        {
            MyStat = GetComponent<Stat>();
            cosHalfFov = Mathf.Cos((sightAngle * 0.5f) * Mathf.Deg2Rad);
        }

        protected virtual void Start()
        {
            MyStat.OnDead += OnDead;
        }

        protected virtual void OnDestroy()
        {
            MyStat.OnDead -= OnDead;
        }

        protected virtual void Update()
        {
            UpdateLostTarget();
            UpdateLastSeenPosition(); 
            UpdateAlertTimer();
        }

        protected virtual void FixedUpdate()
        {
            CheckHostileInSight();
            CheckTargetInSight();
            CheckTargetIsValid();
        }

        private void UpdateAlertTimer()
        {
            if (!IsAlert) return;
            if (HasTarget)
            {
                alertTimer = 0f;
            }
            else
            {
                alertTimer += Time.deltaTime;

                if (alertTimer >= alertDuration)
                {
                    IsAlert = false;
                }
            }
        }

        private void UpdateLostTarget()
        {
            if (!HasTarget) return;
            if (!TargetVisible)
            {
                ResetTarget();
            }
        }

        private void UpdateLastSeenPosition()
        {
            if (!TargetVisible) return;
            LastSeenPosition = CurrentTargetHead.position;
        }

        protected virtual void SetTarget(Transform target)
        {
            CurrentTarget = target;
            if (target.TryGetComponent<Stat>(out var stat)) 
                CurrentTargetStat = stat;
            if (target.TryGetComponent<Animator>(out var anim))
                CurrentTargetHead = anim.GetBoneTransform(HumanBodyBones.Head);

            LastSeenPosition = CurrentTargetHead.position;
            IsAlert = true;
        }

        protected virtual void ResetTarget()
        {
            CurrentTarget = null;
            CurrentTargetStat = null;
            CurrentTargetHead = null;
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
            if (HasTarget) return;

            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                sightRange,
                sightBuffer,
                enemyLayer
            );

            if (hitCount == 0)
            {
                ResetTarget();
                return;
            }

            Transform best = SelectBestVisibleTarget(hitCount);

            if (best != null) SetTarget(best);
            else ResetTarget();
        }

        private Transform SelectBestVisibleTarget(int hitCount)
        {
            Vector3 origin = transform.position;
            Vector3 originEye = origin + Vector3.up * visibleOffesetHight;

            Transform best = null;
            float bestScore = float.MinValue;

            for (int i = 0; i < hitCount; i++)
            {
                Transform candidate = sightBuffer[i]?.transform;
                if (candidate == null) continue;

                if (!IsInSightAngle(candidate, origin))
                    continue;

                if (!HasLineOfSight(originEye, candidate))
                    continue;

                float score = CalculateTargetScore(candidate, origin);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        private bool IsInSightAngle(Transform target, Vector3 origin)
        {
            Vector3 dir = target.position - origin;
            dir.y = 0f;

            float sqrDist = dir.sqrMagnitude;
            if (sqrDist > sightRange * sightRange)
                return false;

            dir.Normalize();

            return Vector3.Dot(transform.forward, dir) >= cosHalfFov;
        }

        private bool HasLineOfSight(Vector3 originEye, Transform target)
        {
            Vector3 targetEye = target.position + Vector3.up * visibleOffesetHight;

            Vector3 dir = targetEye - originEye;
            float dist = dir.magnitude;
            dir /= dist;

            int hit = Physics.RaycastNonAlloc(
                originEye,
                dir,
                rayHits,
                dist,
                WorldManager.Instance.GetLevelLayers()
            );

            return hit == 0;
        }

        private float CalculateTargetScore(Transform target, Vector3 origin)
        {
            Vector3 flat = target.position - origin;
            flat.y = 0f;

            float dist = flat.magnitude;
            float distScore = (1f / (1f + dist)) * 10f;

            Vector3 dir = flat / dist;
            float dot = Vector3.Dot(transform.forward, dir);
            float angleScore = dot * 5f;

            return distScore + angleScore;
        }

        protected void CheckTargetInSight()
        {
            if (!HasTarget) return;

            Vector3 originEye = myEyes.position;
            Vector3 targetEye = CurrentTargetHead.position;

            Vector3 dir = targetEye - originEye;
            float dist = dir.magnitude;
            dir /= dist;

            int hit = Physics.SphereCastNonAlloc(
                originEye,
                0.15f,
                dir,
                rayHits,
                dist,
                WorldManager.Instance.GetLevelLayers()
            );

            bool visible = (hit == 0);
            TargetVisible = visible;
        }

        protected void CheckTargetIsValid()
        {
            if (!HasTarget) return;

            if (CurrentTargetStat.IsDead) ResetTarget();
        }
        #endregion

        private void OnDead()
        {
            ResetTarget();
            ResetCapture();
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
