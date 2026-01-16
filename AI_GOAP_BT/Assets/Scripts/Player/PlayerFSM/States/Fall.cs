using UnityEngine;

namespace Player.FSM
{
    public class Fall : BasePlayerState
    {
        Vector3 fallDirection;

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

            fallDirection = ctx.PlayerCC.velocity;
            fallDirection.y = 0;
            fallDirection.Normalize();

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
            if (ctx.IsGrounded && ctx.StateTime >= 0.1f) return PlayerState.Land;
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
            return fallDirection * ctx.OnAirSpeed;
        }

        protected override void CalculatePlayerTransform()
        {
            if (ctx.PlayerCC.enabled)
                ctx.PlayerCC.Move(ctx.PlayerVelocity * Time.deltaTime);
        }
    }
}
