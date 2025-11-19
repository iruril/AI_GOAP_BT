using UnityEngine;

namespace Sensor.Assualt
{
    public class AssaultSensor : ActorSensorBase
    {
        protected override void Awake()
        {
            base.Awake();
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

            if(timer >= 1f && TargetVisible)
            {
                CurrentTargetStat.ApplyDamage(10f);
                timer = 0f;
            }
        }
    }
}
