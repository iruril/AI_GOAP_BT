using System;
using UnityEngine;

namespace Sensor.Assualt
{
    public class AssaultSensor : ActorSensorBase
    {
        public event Action<Transform> OnTargetSet;
        public event Action OnTargetReset;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Update()
        {
            base.Update();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
        }

        protected override void SetTarget(Transform target)
        {
            base.SetTarget(target);
            OnTargetSet?.Invoke(target);
        }

        protected override void ResetTarget()
        {
            base.ResetTarget();
            OnTargetReset?.Invoke();
        }
    }
}
