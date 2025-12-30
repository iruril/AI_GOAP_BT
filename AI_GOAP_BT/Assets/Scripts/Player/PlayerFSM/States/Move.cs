using UnityEngine;

namespace Player.FSM
{
    public class Move : BasePlayerState
    {
        public Move(PlayerController ctx, PlayerState key) : base(ctx, key)
        {
        }

        public override void EnterState()
        {
            base.EnterState();
            ctx.Anim.applyRootMotion = true;
            ctx.Anim.CrossFade(AnimHash.Strafe, 0.25f);
        }

        public override void ExitState()
        {
        }

        public override void UpdateState()
        {
            base.UpdateState();
            RotatePlayer();
            CalculatePlayerTransform();
        }

        public override void PhysicsUpdateState()
        {
            ctx.Anim.SetFloat(AnimHash.XAxis, ctx.Input.HorizontalInput);
            ctx.Anim.SetFloat(AnimHash.YAxis, ctx.Input.VerticalInput);
            base.PhysicsUpdateState();
        }

        public override PlayerState GetNextState()
        {
            if (ctx.Input.Jump && !ctx.IsChatting) return PlayerState.Jump;
            if (!ctx.IsGrounded) return PlayerState.Fall;
            if (ctx.Input.MoveInputMap == Vector2.zero)
            {
                return PlayerState.Idle;
            }
            if (IsOnTurnOppsiteCondition())
                return PlayerState.TurnOpposite;
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

        private void RotatePlayer()
        {
            float step = Time.deltaTime * 5f;
            float yRotation;
            yRotation = Vector3.SignedAngle(Vector3.forward, ctx.PlayerForward, Vector3.up);

            ctx.transform.rotation =
                Quaternion.Slerp
                (
                    ctx.transform.rotation,
                    Quaternion.Euler(0, yRotation, 0),
                    step
                );
        }

        private bool IsOnTurnOppsiteCondition()
        {
            if (ctx.Input.Aim) return false;
            if (!MathUtility.IsSameDirection(ctx.transform.forward, ctx.PlayerXZVelocity.normalized, 45f)) 
                return false;

            if (ctx.DeltaYaw > 720)
                return true;

            Vector3 camYawDir = 
                Quaternion.Euler(0, ctx.CamController.CamTarget.eulerAngles.y, 0)
                * Vector3.forward;
            float angle = Vector3.Angle(ctx.transform.forward, camYawDir);

            return angle >= 120f;
        }
    }
}
