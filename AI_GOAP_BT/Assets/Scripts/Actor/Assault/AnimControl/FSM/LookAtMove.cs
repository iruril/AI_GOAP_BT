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

            if (ctx.AttackedDirection.sqrMagnitude > 0.001f)
            {
                targetDir = ctx.AttackedDirection;
            }
            else if (ctx.MyBrain.Sensor.IsAlert || ctx.MyBrain.Sensor.LastSeenPosition != Vector3.negativeInfinity)
            {
                targetDir = ctx.MyBrain.Sensor.LastSeenPosition - ctx.transform.position;
            }

            targetDir.y = 0;
            if (targetDir.sqrMagnitude <= Mathf.Epsilon)
                return;
            targetDir.Normalize();

            float step = Time.fixedDeltaTime * 5f;
            float yRotation = Vector3.SignedAngle(Vector3.forward, targetDir, Vector3.up);

            if (float.IsNaN(yRotation)) return;

            Quaternion targetRot = Quaternion.Euler(0, yRotation, 0);
            ctx.MyRigid.MoveRotation(Quaternion.Slerp(ctx.MyRigid.rotation, targetRot, step));
        }
    }
}
