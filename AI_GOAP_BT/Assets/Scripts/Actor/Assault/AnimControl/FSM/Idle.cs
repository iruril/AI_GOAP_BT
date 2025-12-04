using UnityEngine;
using MEC;
using System.Collections.Generic;

namespace AnimControl.Assault
{
    public class Idle : BaseAssaultAnimState
    {
        bool turning = false;
        CoroutineHandle turnHandle;

        public Idle(AssaultAnimFSM ctx, AnimState key) : base(ctx, key)
        {
            this.ctx = ctx;
        }

        public override void EnterState()
        {
            base.EnterState();
            ctx.SetTargetAccel(0f);
            ctx.MyBrain.Navigator.AI.enableRotation = false;
            ctx.RootRotation = false;
            ctx.Anim.CrossFade(AnimHash.Strafe, 0.25f);
            turning = false;
        }

        public override void ExitState()
        {
            Timing.KillCoroutines(turnHandle);
        }

        public override void UpdateState()
        {
            base.UpdateState();
        }

        public override void PhysicsUpdateState()
        {
            PivotTurnHandler();
            ctx.LookHitDirection();
        }

        public override AnimState GetNextState()
        {
            if (Vector3.Distance(ctx.transform.position, ctx.MyBrain.Navigator.AI.endOfPath) > 1.5f)
            {
                if (!ctx.MyBrain.Sensor.HasTarget)
                    return AnimState.Start;
                else
                    return AnimState.LookAtMove;
            }
            return StateKey;
        }

        public override void OnTriggerEnter(Collider other) { }

        public override void OnTriggerStay(Collider other) { }

        public override void OnTriggerExit(Collider other) { }

        private void PivotTurnHandler()
        {
            if (turning) return;
            Vector3 targetDir = ctx.MyBrain.Sensor.LastSeenPosition - ctx.transform.position;
            targetDir.y = 0;

            if (targetDir.sqrMagnitude < 0.001f)
                return;

            targetDir.Normalize();

            bool onAim = ctx.MyBrain.Sensor.HasTarget;

            if (MathUtility.IsRightDirection(ctx.transform.forward, targetDir, 60))
            {
                turnHandle = Timing.RunCoroutine(DoTurn(false, onAim, targetDir));
            }
            else if (MathUtility.IsLeftDirection(ctx.transform.forward, targetDir, 60))
            {
                turnHandle = Timing.RunCoroutine(DoTurn(true, onAim, targetDir));
            }
        }

        private IEnumerator<float> DoTurn(bool leftTurn, bool onAim, Vector3 targetDir)
        {
            float animTime = onAim ? 0.66f : 1.47f;

            turning = true;
            Quaternion startRot = ctx.transform.rotation;
            Quaternion endRot = Quaternion.LookRotation(targetDir);

            int turnHash =
                leftTurn
                ? (onAim ? AnimHash.AimTurn_L : AnimHash.Turn_L)
                : (onAim ? AnimHash.AimTurn_R : AnimHash.Turn_R);

            ctx.Anim.CrossFade(turnHash, 0.05f);

            float time = 0;
            while(time <= animTime)
            {
                float t = time / animTime;
                Quaternion newRot = Quaternion.Slerp(startRot, endRot, t);
                ctx.MyRigid.MoveRotation(newRot);

                time += Time.deltaTime;
                yield return Timing.WaitForOneFrame;
            }

            ctx.MyRigid.MoveRotation(endRot);
            ctx.Anim.CrossFade(AnimHash.Strafe, 0.05f);
            turning = false;
        }
    }
}
