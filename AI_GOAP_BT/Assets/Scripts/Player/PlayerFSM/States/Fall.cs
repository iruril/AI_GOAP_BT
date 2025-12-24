using UnityEngine;

namespace Player.FSM
{
    public class Fall : BasePlayerState
    {
        public Fall(PlayerController ctx, PlayerState key) : base(ctx, key)
        {
        }

        public override void EnterState()
        {
            base.EnterState();
            ctx.Anim.applyRootMotion = false;

            PlayerState prevState = ctx.GetPrevState();
            if (prevState != PlayerState.Jump)
            {
                ctx.CalculateOnAirSpeed();
            }

            ctx.Anim.CrossFadeInFixedTime(AnimHash.Fall, 0.25f);
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
            if (ctx.IsGrounded) return PlayerState.Land;
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

        private Vector3 GetXZVelocity()
        {
            Vector3 velocity = ctx.PlayerForward * ctx.Input.VerticalInput + ctx.PlayerRight * ctx.Input.HorizontalInput;
            Vector3 direction = velocity.normalized;

            float moveSpeed = Mathf.Min(velocity.magnitude, 1.0f) * ctx.OnAirSpeed;

            return direction * moveSpeed;
        }

        protected override void CalculatePlayerTransform()
        {
            ctx.PlayerCC.Move(ctx.PlayerVelocity * Time.deltaTime);
        }
    }
}
