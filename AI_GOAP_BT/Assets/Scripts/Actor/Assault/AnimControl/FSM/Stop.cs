using UnityEngine;

namespace AnimControl.Assault
{
    public class Stop : AssaultAnimState
    {
        private float stopTime;

        public Stop(AssaultAnimFSM ctx, AnimState key) : base(ctx, key)
        {
            this.ctx = ctx;
        }

        public override void EnterState()
        {
            base.EnterState();
            ctx.Navigator.AI.enableRotation = false;
            int snapSpeed = Mathf.Clamp(Mathf.RoundToInt(ctx.Accel), 1, 4);
            ctx.Anim.SetFloat(AnimHash.SpeedOnStop, snapSpeed);
            ctx.Anim.CrossFade(AnimHash.Stop_R, 0.25f);

            switch (snapSpeed)
            {
                case 3:
                    stopTime = 2.1f;
                    break;
                case 4:
                    stopTime = 2.5f;
                    break;
                default:
                    stopTime = 1.95f;
                    break;
            }
        }

        public override void ExitState()
        {

        }

        public override void UpdateState()
        {
            base.UpdateState();
        }

        public override void PhysicsUpdateState()
        {

        }

        public override AnimState GetNextState()
        {
            if (ctx.StateTime >= stopTime) return AnimState.Idle;
            return StateKey;
        }

        public override void OnTriggerEnter(Collider other) { }

        public override void OnTriggerStay(Collider other) { }

        public override void OnTriggerExit(Collider other) { }
    }
}