using MEC;
using System.Collections.Generic;
using UnityEngine;

namespace Sensor
{
    public abstract class ActorSensorBase : MonoBehaviour, IDamageable
    {
        [Header("Target Info")]
        public Transform CurrentTarget { get; set; }
        public bool HasTarget => CurrentTarget != null;

        public bool TargetVisible { get; set; } = false;
        public float TargetDistance { get; set; } = Mathf.Infinity;
        [Header("Target Memory")]
        public Vector3 LastSeenPosition { get; private set; }
        public bool HasLastSeenPosition { get; private set; } = false;

        [SerializeField] private float loseTargetAfter = 2f;
        protected float targetLostTimer = 0f;

        [Header("Cover Info")]
        public float CoverDistance { get; set; } = Mathf.Infinity;
        public bool UnderAttack { get; set; } = false;
        public bool CoverAvailable { get; set; } = false;

        [Header("Capture Info")]
        public CapturePoint.CapturePoint CurrentCap { get; private set; }
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
        [SerializeField] private float maxHP = 100f;
        protected float currentHP;
        public bool IsDead { get; set; } = false;

        protected CoroutineHandle underAttackHandle;

        protected virtual void Awake()
        {
            currentHP = maxHP;
            cosHalfFov = Mathf.Cos((sightAngle * 0.5f) * Mathf.Deg2Rad);
        }

        protected virtual void Update()
        {
            UpdateTargetDistance();
            UpdateLostTargetTimer();
        }

        protected virtual void FixedUpdate()
        {
            CheckHostileInSight();
            CheckTargetInSight();
        }

        private void UpdateTargetDistance()
        {
            if (!HasTarget)
            {
                TargetDistance = Mathf.Infinity;
                return;
            }

            TargetDistance = Vector3.Distance(transform.position, CurrentTarget.position);
        }

        private void UpdateLostTargetTimer()
        {
            if (!HasTarget) return;

            if (TargetVisible)
            {
                targetLostTimer = 0f;
            }
            else
            {
                targetLostTimer += Time.deltaTime;

                if (targetLostTimer >= loseTargetAfter)
                {
                    CurrentTarget = null;
                    TargetVisible = false;
                }
            }
        }

        protected virtual void SetTarget(Transform target)
        {
            CurrentTarget = target;
            targetLostTimer = 0f;
        }

        protected virtual void ResetTarget()
        {
            CurrentTarget = null;
        }

        protected virtual void NotifySuppressed()
        {
            UnderAttack = true;
            Timing.KillCoroutines(underAttackHandle);
            underAttackHandle = Timing.RunCoroutine(_ResetUnderAttackTimer());
        }

        private IEnumerator<float> _ResetUnderAttackTimer()
        {
            yield return Timing.WaitForSeconds(1.0f);
            UnderAttack = false;
        }

        public virtual void SearchCoverPosition()
        {
            bool result = false;
            //EQS 사용해서 검출할 것.

            CoverAvailable = result;
        }

        #region Damageable Field
        public virtual void ApplyDamage(float dmg)
        {
            currentHP -= dmg;

            if (currentHP <= 0f)
            {
                currentHP = 0f;
                IsDead = true;
            }
        }
        #endregion

        #region Capture Field
        public void ResetCapture()
        {
            CurrentCap = null;
        }

        public void GetClosestCapture(out Vector3 destination)
        {
            CurrentCap = WorldManager.Instance.RequestClosestCapture(transform, captureOffsetRadius, out destination);
        }

        public bool IsCurrentCapCapturerd()
        {
            return !CurrentCap.NeedToCapture(transform);
        }
        #endregion

        #region Sight Check & Assgin Target Field
        private void CheckHostileInSight()
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
                CurrentTarget = null;
                return;
            }

            Transform bestTarget = SelectBestVisibleTarget(hitCount);

            if (bestTarget != null)
            {
                SetTarget(bestTarget);
            }
            else
            {
                ResetTarget();
            }
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

        private void CheckTargetInSight()
        {
            if (!HasTarget)
            {
                TargetVisible = false;
                return;
            }

            Vector3 originEye = transform.position + Vector3.up * visibleOffesetHight;
            Vector3 targetEye = CurrentTarget.position + Vector3.up * visibleOffesetHight;

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

            bool visible = (hit == 0);
            TargetVisible = visible;

            if (visible)
            {
                LastSeenPosition = CurrentTarget.position;
                HasLastSeenPosition = true;
            }
        }
        #endregion

        private void OnDrawGizmos()
        {
            if (HasTarget)
            {
                if (transform.CompareTag("TeamBlue")) Gizmos.color = Color.cyan;
                else Gizmos.color = Color.magenta;

                Vector3 originEye = transform.position + Vector3.up * visibleOffesetHight;
                Vector3 targetEye = CurrentTarget.position + Vector3.up * visibleOffesetHight;

                Gizmos.DrawWireSphere(originEye, 0.3f);
                Gizmos.DrawWireSphere(targetEye, 0.3f);
                Gizmos.DrawLine(originEye, targetEye);
            }
        }
    }
}
