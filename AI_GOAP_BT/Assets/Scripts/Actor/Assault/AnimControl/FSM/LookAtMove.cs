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
            ctx.Anim.CrossFade(AnimHash.Strafe, 0.1f);
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
            if (ctx.MyBrain.Navigator.AI.velocity.sqrMagnitude < 0.001f)
                return AnimState.Idle;
            if (!ctx.MyBrain.Sensor.IsAlert)
            {
                if (ctx.MyBrain.Navigator.AI.velocity.sqrMagnitude > 0.001f)
                    return AnimState.Move;
                else
                    return AnimState.Idle;
            }
            if(Vector3.Distance(ctx.transform.position, ctx.MyBrain.Navigator.AI.endOfPath) <= 0.1f)
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
            Vector3 targetDir = Vector3.zero;
            Quaternion targetRot = ctx.MyRigid.rotation;
            bool hasValidDir = false;

            if(ctx.AttackedDirection.sqrMagnitude > 0.001f)
            {
                targetDir = ctx.AttackedDirection;
                hasValidDir = true;
            }
            else if(ctx.MyBrain.Sensor.IsAlert || ctx.MyBrain.Sensor.LastSeenPosition != Vector3.negativeInfinity)
            {
                targetDir = ctx.MyBrain.Sensor.LastSeenPosition - ctx.transform.position;
                targetDir.y = 0;
                targetDir.Normalize();

                if(targetDir.sqrMagnitude > 0.001f) hasValidDir = true;
            }

            if (!hasValidDir) return;

            targetRot = Quaternion.LookRotation(targetDir, Vector3.up);
            float maxStep = ctx.MyBrain.Sensor.MyStat.RotateSpeedToTarget * Time.fixedDeltaTime;

            Quaternion newRot = Quaternion.RotateTowards(ctx.MyRigid.rotation, targetRot, maxStep);
            ctx.MyRigid.MoveRotation(newRot);
        }
    }
}
