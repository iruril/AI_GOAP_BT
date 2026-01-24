using MEC;
using System.Collections.Generic;
using UnityEngine;

namespace Player.FSM
{
    public class CrouchIdle : BasePlayerState
    {
        bool turning = false;
        CoroutineHandle turnHandle;

        public CrouchIdle(PlayerController ctx, PlayerState key) : base(ctx, key)
        {
        }

        public override void EnterState()
        {
            base.EnterState();
            ctx.Anim.applyRootMotion = true;
            ctx.Anim.CrossFadeInFixedTime(AnimHash.Crouch, 0.25f);
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
            if (!ctx.IsGrounded) return PlayerState.Fall;
            if (!ctx.Input.Crouch) return PlayerState.Idle;
            if (ctx.Input.MoveInputMap != Vector2.zero) return PlayerState.Crouch;
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

            if (MathUtility.IsRightDirectionXZ(ctx.transform.forward, targetDir, 60))
            {
                Timing.KillCoroutines(turnHandle);
                turnHandle = Timing.RunCoroutine(DoTurn(false, ctx.Input.Aim), Segment.FixedUpdate);
            }
            else if (MathUtility.IsLeftDirectionXZ(ctx.transform.forward, targetDir, 60))
            {
                Timing.KillCoroutines(turnHandle);
                turnHandle = Timing.RunCoroutine(DoTurn(true, ctx.Input.Aim), Segment.FixedUpdate);
            }
        }

        private IEnumerator<float> DoTurn(bool leftTurn, bool onAim)
        {
            float animTime;
            float step = Time.deltaTime * 5f;

            turning = true;

            int turnHash;
            if (onAim)
            {
                animTime = 0.55f;
                turnHash = leftTurn ? AnimHash.AimCrouchTurn_L : AnimHash.AimCrouchTurn_R;
            }
            else
            {
                animTime = 0.55f;
                turnHash = leftTurn ? AnimHash.CrouchTurn_L : AnimHash.CrouchTurn_R;
            }

            ctx.Anim.applyRootMotion = false;
            ctx.Anim.CrossFadeInFixedTime(turnHash, 0.1f);

            float time = 0;
            while (time <= animTime)
            {
                if (ctx.MyStat.IsDead) yield break;

                float yRotation = Vector3.SignedAngle(Vector3.forward, ctx.PlayerForward, Vector3.up);
                ctx.transform.rotation =
                    Quaternion.Slerp
                    (
                        ctx.transform.rotation,
                        Quaternion.Euler(0, yRotation, 0),
                        step
                    );

                time += Timing.DeltaTime;
                yield return Timing.WaitForOneFrame;
            }

            turning = false;
            ctx.Anim.applyRootMotion = true;
            ctx.Anim.CrossFadeInFixedTime(AnimHash.Crouch, 0.1f);
        }
    }
}

