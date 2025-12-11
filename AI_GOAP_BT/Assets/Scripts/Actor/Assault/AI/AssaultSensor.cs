using System;
using UnityEngine;

namespace Sensor.Assualt
{
    public class AssaultSensor : ActorSensorBase
    {
        //private GOAP.Assualt.AssaultBrain myBrain;
        public event Action<Transform> OnTargetSet;
        public event Action OnTargetReset;

        protected override void Awake()
        {
            base.Awake();
            //myBrain = GetComponent<GOAP.Assualt.AssaultBrain>();
        }

        protected override void Start()
        {
            base.Start();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
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
            if (!isServer) return;
            base.SetTarget(target);
            OnTargetSet?.Invoke(target);
        }

        protected override void ResetTarget()
        {
            if (!isServer) return;
            base.ResetTarget();
            OnTargetReset?.Invoke();
        }
    }
}
