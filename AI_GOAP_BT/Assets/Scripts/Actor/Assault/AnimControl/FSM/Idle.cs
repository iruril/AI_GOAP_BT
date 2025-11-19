using BehaviorDesigner.Runtime.Tasks.Unity.UnityQuaternion;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AnimControl.Assault
{
    public class Idle : BaseAssaultAnimState
    {
        public Idle(AssaultAnimFSM ctx, AnimState key) : base(ctx, key)
        {
            this.ctx = ctx;
        }

        public override void EnterState()
        {
            base.EnterState();
            ctx.SetTargetAccel(0f);
            ctx.Navigator.AI.enableRotation = false;
            ctx.RootRotation = false;
            ctx.Anim.CrossFade(AnimHash.Strafe, 0.15f);
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
            ctx.LookHitDirection();
        }

        public override AnimState GetNextState()
        {
            if (Vector3.Distance(ctx.transform.position, ctx.Navigator.AI.endOfPath) > 1.5f)
            {
                if (!ctx.MySensor.HasTarget)
                    return AnimState.Start;
                else
                    return AnimState.LookAtMove;
            }
            return StateKey;
        }

        public override void OnTriggerEnter(Collider other) { }

        public override void OnTriggerStay(Collider other) { }

        public override void OnTriggerExit(Collider other) { }

        void LookAtTarget()
        {
            if (!ctx.MySensor.HasTarget) return;

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
