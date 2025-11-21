using UnityEngine;

namespace Sensor.Assualt
{
    public class AssaultSensor : ActorSensorBase
    {
        AnimControl.Assault.AssaultAnimFSM animStatus;
        GunHandler gunHandler;

        protected override void Awake()
        {
            base.Awake();
            animStatus = GetComponent<AnimControl.Assault.AssaultAnimFSM>();
            gunHandler = GetComponent<GunHandler>();
        }

        protected override void Update() 
        { 
            base.Update(); 
            AttackTest();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
        }

        float timer = 0f;
        void AttackTest()
        {
            timer += Time.deltaTime;
            if (!HasTarget) return;
            if (timer >= 1f && animStatus.AimWeight >= 1f)
            {
                gunHandler.Fire();
                CurrentTargetStat.ApplyDamage(10f, transform.position);
                timer = 0f;
            }
        }
    }
}
