using UnityEngine;

namespace AnimControl.Assault
{
    public class TurnOpposite : AssaultAnimState
    {
        private float turnTime;

        public TurnOpposite(AssaultAnimFSM ctx, AnimState key) : base(ctx, key)
        {
            this.ctx = ctx;
        }

        public override void EnterState()
        {
            base.EnterState();
            ctx.Navigator.AI.enableRotation = false;
            ctx.RootRotation = true;
            int snapSpeed = Mathf.Clamp(Mathf.RoundToInt(ctx.Accel), 1, 4);
            ctx.Anim.SetFloat(AnimHash.TransitionAccel, snapSpeed);
            ctx.Anim.CrossFade(AnimHash.Opposite_R, 0.1f);

            switch (snapSpeed)
            {
                case 3:
                    turnTime = 1.9f;
                    break;
                case 4:
                    turnTime = 1.1f;
                    break;
                default:
                    turnTime = 1.3f;
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
            if (ctx.StateTime >= turnTime) return AnimState.Move;
            return StateKey;
        }

        public override void OnTriggerEnter(Collider other) { }

        public override void OnTriggerStay(Collider other) { }

        public override void OnTriggerExit(Collider other) { }
    }
}
