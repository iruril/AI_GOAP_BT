using System;
using UnityEngine;

namespace Sensor.Assualt
{
    public class AssaultSensor : ActorSensorBase
    {
        protected override void Awake()
        {
            base.Awake();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
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
        }

        protected override void ResetTarget()
        {
            base.ResetTarget();
        }
    }
}
