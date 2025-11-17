using UnityEngine;
using MEC;
using System.Collections.Generic;

namespace Sensor
{
    public abstract class ActorSensorBase : MonoBehaviour, IDamageable
    {
        [Header("Target Info")]
        public Transform CurrentTarget;
        public bool HasTarget => CurrentTarget != null;

        public bool TargetVisible = false;
        public float TargetDistance = Mathf.Infinity;

        [Header("Cover Info")]
        public float CoverDistance = Mathf.Infinity;
        public bool UnderAttack = false;
        public bool CoverAvailable = false;

        [Header("Capture Info")]
        public CapturePoint.CapturePoint CurrentCap { get; private set; }
        public float captureOffsetRadius = 4f;

        [Header("Combat Info")]
        public float MaxHP = 100f;
        protected float currentHP;

        public bool IsDead = false;

        public float MaximunRange = 30f;
        public float EffectiveRange = 15f;

        [Header("Timers")]
        public float FindTargetTimeWhenLost = 10f;

        protected CoroutineHandle underAttackHandle;

        protected virtual void Awake()
        {
            currentHP = MaxHP;
        }

        protected virtual void Update()
        {
            UpdateTargetDistance();
            UpdateLostTargetTimer();
        }

        protected void UpdateTargetDistance()
        {
            if (CurrentTarget == null)
            {
                TargetDistance = Mathf.Infinity;
                return;
            }

            TargetDistance = Vector3.Distance(transform.position, CurrentTarget.position);
        }

        protected void UpdateLostTargetTimer()
        {
            if (TargetVisible)
                FindTargetTimeWhenLost = 0f;
            else
                FindTargetTimeWhenLost += Time.deltaTime;
        }

        public virtual void SetTarget(Transform target)
        {
            CurrentTarget = target;
            FindTargetTimeWhenLost = 0f;
        }

        public virtual void NotifySuppressed()
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

        public void ApplyDamage(float dmg)
        {
            currentHP -= dmg;

            if (currentHP <= 0f)
            {
                currentHP = 0f;
                IsDead = true;
            }
        }

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
    }
}
