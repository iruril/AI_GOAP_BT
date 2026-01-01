using MEC;
using System.Collections.Generic;
using UnityEngine;

namespace Player.FSM
{
    public class Idle : BasePlayerState
    {
        bool turning = false;
        CoroutineHandle turnHandle;

        public Idle(PlayerController ctx, PlayerState key) : base(ctx, key)
        {
        }

        public override void EnterState()
        {
            base.EnterState();
            ctx.Anim.applyRootMotion = true;
            ctx.Anim.CrossFadeInFixedTime(AnimHash.Strafe, 0.25f);
            turning = false;
        }

        public override void ExitState()
        {
            Timing.KillCoroutines(turnHandle);
        }

        public override void UpdateState()
        {
            base.UpdateState();
            CalculatePlayerTransform();
        }

        public override void PhysicsUpdateState()
        {
            base.PhysicsUpdateState();
            PivotTurnHandler();
        }

        public override PlayerState GetNextState()
        {
            if (ctx.Input.Jump && !GameManager.GetInstance().InputMap.IsOnUIAction) return PlayerState.Jump;
            if (!ctx.IsGrounded) return PlayerState.Fall;
            if (ctx.Input.MoveInputMap != Vector2.zero)
            {
                return PlayerState.Move;
            }
            return StateKey;
        }

        public override void OnTriggerEnter(Collider other)
        {
        }

        public override void OnTriggerExit(Collider other)
        {
        }

        public override void OnTriggerStay(Collider other)
        {
        }

        private void PivotTurnHandler()
        {
            if (turning) return;

            Vector3 targetDir = ctx.CamController.CamTarget.forward;
            targetDir.y = 0;
            targetDir.Normalize();

            if (MathUtility.IsRightDirection(ctx.transform.forward, targetDir, 60))
            {
                turnHandle = Timing.RunCoroutine(DoTurn(false, targetDir, ctx.Input.Aim), Segment.FixedUpdate);
            }
            else if (MathUtility.IsLeftDirection(ctx.transform.forward, targetDir, 60))
            {
                turnHandle = Timing.RunCoroutine(DoTurn(true, targetDir, ctx.Input.Aim), Segment.FixedUpdate);
            }
        }

        private IEnumerator<float> DoTurn(bool leftTurn, Vector3 targetDir, bool onAim)
        {
            float animTime;

            turning = true;
            Quaternion startRot = ctx.transform.rotation;
            Quaternion endRot = Quaternion.LookRotation(targetDir);

            int turnHash;
            if (onAim)
            {
                animTime = 0.66f;
                turnHash = leftTurn ? AnimHash.AimTurn_L : AnimHash.AimTurn_R;
            }
            else
            {
                animTime = 0.86f;
                turnHash = leftTurn ? AnimHash.Turn_L : AnimHash.Turn_R;
            }

            ctx.Anim.applyRootMotion = false;
            ctx.Anim.CrossFadeInFixedTime(turnHash, 0.1f);

            float time = 0;
            while (time <= animTime)
            {
                if (ctx.MyStat.IsDead) yield break;

                float t = time / animTime;
                ctx.transform.rotation = Quaternion.Slerp(startRot, endRot, t);

                time += Timing.DeltaTime;
                yield return Timing.WaitForOneFrame;
            }

            ctx.transform.rotation = endRot;
            turning = false;
            ctx.Anim.applyRootMotion = true;
            ctx.Anim.CrossFadeInFixedTime(AnimHash.Strafe, 0.1f);
        }
    }
}
