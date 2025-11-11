using UnityEngine;

namespace AnimControl.Assault
{
    public class Move : AssaultAnimState
    {
        private float stoppingDistance;

        public Move(AssaultAnimFSM ctx, AnimState key) : base(ctx, key)
        {
            this.ctx = ctx;
        }

        public override void EnterState()
        {
            base.EnterState();
            ctx.Navigator.AI.enableRotation = true;
            ctx.RootRotation = false;
            ctx.Anim.CrossFade(AnimHash.Strafe, 0.15f);
        }

        public override void ExitState()
        {

        }

        public override void UpdateState()
        {
            base.UpdateState();
            DecideStopDistance();
        }

        public override void PhysicsUpdateState()
        {

        }

        public override AnimState GetNextState()
        {
            if (Vector3.Distance(ctx.transform.position, ctx.Navigator.AI.endOfPath) <= stoppingDistance)
                return AnimState.Stop;
            if (IsOnTurnOppsiteCondition())
                return AnimState.TurnOpposite;
            return StateKey;
        }

        public override void OnTriggerEnter(Collider other) { }

        public override void OnTriggerStay(Collider other) { }

        public override void OnTriggerExit(Collider other) { }

        void DecideStopDistance()
        {
            int snapAccel = Mathf.Clamp(Mathf.RoundToInt(ctx.Accel), 1, 4);
            switch (snapAccel)
            {
                case 3:
                    stoppingDistance = 3f;
                    break;
                case 4:
                    stoppingDistance = 4.5f;
                    break;
                default:
                    stoppingDistance = 0.7f;
                    break;
            }
        }

        bool IsOnTurnOppsiteCondition()
        {
            Vector3 vel = ctx.Navigator.AI.desiredVelocity;
            Vector3 tgt = ctx.Navigator.AI.steeringTarget - ctx.transform.position;

            vel.y = 0f;
            tgt.y = 0f;

            vel.Normalize();
            tgt.Normalize();

            if (Vector3.Angle(vel, tgt) >= 135f)
                return true;
            return false;
        }
    }
}