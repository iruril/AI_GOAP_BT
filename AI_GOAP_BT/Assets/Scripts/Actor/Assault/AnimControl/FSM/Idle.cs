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
            ctx.Anim.CrossFade(AnimHash.Strafe, 0.1f);
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
        }

        public override AnimState GetNextState()
        {
            if (Vector3.Distance(ctx.transform.position, ctx.MyBrain.Navigator.AI.endOfPath) > 0.7f)
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

            Vector3 targetDir;

            if (ctx.MyBrain.Sensor.HasTarget)
            {
                targetDir = ctx.MyBrain.Sensor.LastSeenPosition - ctx.transform.position;
                targetDir.y = 0;
                targetDir.Normalize();
            }
            else
            {
                targetDir = ctx.AttackedDirection;
            }

            if (targetDir.sqrMagnitude < 0.001f)
                return;

            if (MathUtility.IsRightDirection(ctx.transform.forward, targetDir, 60))
            {
                turnHandle = Timing.RunCoroutine(DoTurn(false, targetDir));
            }
            else if (MathUtility.IsLeftDirection(ctx.transform.forward, targetDir, 60))
            {
                turnHandle = Timing.RunCoroutine(DoTurn(true, targetDir));
            }
        }
        private IEnumerator<float> DoTurn(bool leftTurn, Vector3 targetDir)
        {
            float animTime = 0.66f;

            turning = true;
            Quaternion startRot = ctx.transform.rotation;
            Quaternion endRot = Quaternion.LookRotation(targetDir);

            int turnHash = leftTurn ? AnimHash.AimTurn_L : AnimHash.AimTurn_R;

            ctx.Anim.CrossFadeInFixedTime(turnHash, 0.1f);

            float time = 0;
            while (time <= animTime)
            {
                if (ctx.MyBrain.Sensor.MyStat.IsDead) yield break;

                float t = time / animTime;
                Quaternion newRot = Quaternion.Slerp(startRot, endRot, t);
                ctx.MyRigid.MoveRotation(newRot);

                time += Time.deltaTime;
                yield return Timing.WaitForOneFrame;
            }

            ctx.MyRigid.MoveRotation(endRot);
            ctx.Anim.CrossFadeInFixedTime(AnimHash.Strafe, 0.1f);
            turning = false;
        }
    }
}
