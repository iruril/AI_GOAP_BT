using MEC;
using System.Collections.Generic;
using UnityEngine;

namespace Player.FSM
{
    public class TurnOpposite : BasePlayerState
    {
        private float turnTime;

        public TurnOpposite(PlayerController ctx, PlayerState key) : base(ctx, key)
        {
        }

        public override void EnterState()
        {
            base.EnterState();
            ctx.Anim.applyRootMotion = true;
            ctx.IsOnJumping = false;

            Vector3 tgt = ctx.CamController.CamTarget.forward;
            tgt.y = 0f;
            tgt.Normalize();

            int snapSpeed = Mathf.Clamp(Mathf.RoundToInt(ctx.Anim.GetFloat(AnimHash.Accelation)), 1, 4);
            ctx.Anim.SetFloat(AnimHash.TransitionAccel, snapSpeed);

            if (MathUtility.IsRightDirectionXZ(ctx.transform.forward, tgt, 0f))
            {
                ctx.Anim.CrossFadeInFixedTime(AnimHash.Opposite_R, 0.1f);
            }
            else
            {
                ctx.Anim.CrossFadeInFixedTime(AnimHash.Opposite_L, 0.1f);
            }

            switch (snapSpeed)
            {
                case 4:
                    turnTime = 0.95f;
                    break;
                default:
                    turnTime = 1.1f;
                    break;
            }
        }

        public override void ExitState()
        {
        }

        public override void UpdateState()
        {
            base.UpdateState();
            CalculatePlayerTransform();
        }

        public override void PhysicsUpdateState()
        {
            base.PhysicsUpdateState();
        }

        public override PlayerState GetNextState()
        {
            if (!ctx.IsGrounded) return PlayerState.Fall;
            if (ctx.StateTime >= turnTime)
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

        IEnumerator<float> SmoothRotate(Quaternion startRot, Quaternion endRot, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                ctx.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
                yield return Timing.WaitForOneFrame;
            }

            ctx.transform.rotation = endRot;
        }
    }
}