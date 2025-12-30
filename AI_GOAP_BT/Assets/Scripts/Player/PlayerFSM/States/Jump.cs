using UnityEngine;

namespace Player.FSM
{
    public class Jump : BasePlayerState
    {
        private Quaternion jumpRot;

        public Jump(PlayerController ctx, PlayerState key) : base(ctx, key)
        {
        }

        public override void EnterState()
        {
            base.EnterState();
            ctx.Anim.applyRootMotion = false;
            ctx.CalculateOnAirSpeed();
            ctx.IsOnJumping = true;

            SetPlayerTargetRotation();
            ExecuteJump();
            ctx.Anim.CrossFadeInFixedTime(AnimHash.Jump, 0.1f);
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
            Vector3 xzVelocity = GetXZVelocity();
            float yVelocity = GetYVelocity();
            ctx.PlayerVelocity = new Vector3(xzVelocity.x, yVelocity, xzVelocity.z);
        }

        public override PlayerState GetNextState()
        {
            if (!ctx.IsGrounded && ctx.PlayerCC.velocity.y <= 0)
            {
                return PlayerState.Fall;
            }
            if (ctx.StateTime > 0.4f && ctx.IsGrounded)
            {
                return PlayerState.Idle;
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

        private void ExecuteJump()
        {
            if (ctx.IsGrounded)
            {
                Vector3 playerVelocity = ctx.PlayerVelocity;
                playerVelocity.y = ctx.JumpImpulseVelocity;
                ctx.PlayerVelocity = playerVelocity;
            }
        }

        private Vector3 GetXZVelocity()
        {
            Vector3 direction = jumpRot * Vector3.forward;
            return direction * ctx.OnAirSpeed;
        }

        private void SetPlayerTargetRotation()
        {
            jumpRot = Quaternion.Euler(0,
                    MathUtility.CalculateRotationAngle(ctx.CamController.CamTarget.eulerAngles.y,
                    new Vector2(ctx.Input.RawHorizontalInput, ctx.Input.RawVerticalInput)),
                    0);
        }

        protected override void CalculatePlayerTransform()
        {
            RotatePlayer();
            ctx.PlayerCC.Move(ctx.PlayerVelocity * Time.deltaTime);
        }

        private void RotatePlayer()
        {
            float step = Time.deltaTime * 5f;
            ctx.transform.rotation = Quaternion.Slerp(ctx.transform.rotation, jumpRot, step);
        }
    }
}
