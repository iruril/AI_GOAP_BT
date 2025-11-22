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
            ctx.MyBrain.Navigator.AI.enableRotation = false;
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
            if (!ctx.MyBrain.Sensor.HasTarget)
            {
                if (ctx.MyBrain.Navigator.AI.velocity.sqrMagnitude > 0.001f)
                    return AnimState.Move;
                else return AnimState.Idle;
            }
            if(Vector3.Distance(ctx.transform.position, ctx.MyBrain.Navigator.AI.endOfPath) <= 0.7f)
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
            if(!ctx.MyBrain.Sensor.HasTarget) return;

            Vector3 targetDir = ctx.MyBrain.Sensor.CurrentTarget.position - ctx.transform.position;
            targetDir.y = 0f;
            targetDir.Normalize();

            Quaternion targetRot = Quaternion.LookRotation(targetDir);

            float maxStep = ctx.MyBrain.Sensor.MyStat.RotateSpeedToTarget * Time.fixedDeltaTime;
            Quaternion newRot = Quaternion.RotateTowards(ctx.MyRigid.rotation, targetRot, maxStep);

            ctx.MyRigid.MoveRotation(newRot);
        }
    }
}
