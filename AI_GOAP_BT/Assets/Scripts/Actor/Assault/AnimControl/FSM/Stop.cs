using UnityEngine;

namespace AnimControl.Assault
{
    public class Stop : BaseAssaultAnimState
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
            ctx.RootRotation = false;
            int snapSpeed = Mathf.Clamp(Mathf.RoundToInt(ctx.Accel), 1, 4);
            ctx.Anim.SetFloat(AnimHash.TransitionAccel, snapSpeed);
            ctx.Anim.CrossFade(AnimHash.Stop, 0.1f);

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
            if (ctx.StateTime >= stopTime) 
                return AnimState.Idle;
            if(IsOnTurnOppsiteCondition())
                return AnimState.TurnOpposite;
            return StateKey;
        }

        public override void OnTriggerEnter(Collider other) { }

        public override void OnTriggerStay(Collider other) { }

        public override void OnTriggerExit(Collider other) { }

        bool IsOnTurnOppsiteCondition()
        {
            if (Vector3.Distance(ctx.Navigator.AI.steeringTarget, ctx.transform.position) < 1.5f)
                return false;

            Vector3 vel = ctx.Navigator.AI.desiredVelocity;
            if (vel.sqrMagnitude < 0.001f) return false;

            Vector3 tgt = ctx.Navigator.AI.steeringTarget - ctx.transform.position;

            vel.y = 0f;
            tgt.y = 0f;

            vel.Normalize();
            tgt.Normalize();

            if (Vector3.Angle(vel, tgt) >= 150f)
                return true;
            return false;
        }
    }
}