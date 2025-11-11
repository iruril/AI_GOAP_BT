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
            switch (ctx.Accel)
            {
                case 3:
                    stoppingDistance = 3;
                    break;
                case 4:
                    stoppingDistance = 4.5f;
                    break;
                default:
                    stoppingDistance = 0.7f;
                    break;
            }
            base.UpdateState();
        }

        public override void PhysicsUpdateState()
        {

        }

        public override AnimState GetNextState()
        {
            if (Vector3.Distance(ctx.transform.position, ctx.Navigator.AI.endOfPath) <= stoppingDistance)
                return AnimState.Stop;
            return StateKey;
        }

        public override void OnTriggerEnter(Collider other) { }

        public override void OnTriggerStay(Collider other) { }

        public override void OnTriggerExit(Collider other) { }
    }
}