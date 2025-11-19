using UnityEngine;

namespace AnimControl.Assault
{
    public class LookAtMove : BaseAssaultAnimState
    {
        public LookAtMove(AssaultAnimFSM ctx, AnimState key) : base(ctx, key)
        {
            this.ctx = ctx;
        }

        public override void EnterState()
        {
            base.EnterState();
            ctx.SetTargetAccel(2f);
            ctx.Navigator.AI.enableRotation = false;
            ctx.RootRotation = false;
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
            LookAtTarget();
        }

        public override AnimState GetNextState()
        {
            if (!ctx.MySensor.HasTarget)
            {
                if (ctx.Navigator.AI.velocity.sqrMagnitude > 0.001f)
                    return AnimState.Move;
                else return AnimState.Idle;
            }
            if(Vector3.Distance(ctx.transform.position, ctx.Navigator.AI.endOfPath) <= 0.7f)
            {
                return AnimState.Idle;
            }
            return StateKey;
        }

        public override void OnTriggerEnter(Collider other) { }

        public override void OnTriggerStay(Collider other) { }

        public override void OnTriggerExit(Collider other) { }

        void LookAtTarget()
        {
            if(!ctx.MySensor.HasTarget) return;

            Vector3 targetDir = ctx.MySensor.CurrentTarget.position - ctx.transform.position;
            targetDir.y = 0f;
            targetDir.Normalize();

            Quaternion targetRot = Quaternion.LookRotation(targetDir);

            float maxStep = ctx.MySensor.MyStat.RotateSpeedToTarget * Time.fixedDeltaTime;
            Quaternion newRot = Quaternion.RotateTowards(ctx.MyRigid.rotation, targetRot, maxStep);

            ctx.MyRigid.MoveRotation(newRot);
        }
    }
}
