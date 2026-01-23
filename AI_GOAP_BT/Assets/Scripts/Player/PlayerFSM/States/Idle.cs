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
            if (ctx.Input.Jump && !GameManager.GetInstance().InputMap.IsOnStaticUI) return PlayerState.Jump;
            if (!ctx.IsGrounded) return PlayerState.Fall;
            if (ctx.Input.Crouch) return PlayerState.CrouchIdle;
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
            ctx.Anim.CrossFadeInFixedTime(AnimHash.Strafe, 0.1f);
        }
    }
}
