using FSM;
using UnityEngine;

namespace Player.FSM
{
    public abstract class BasePlayerState : BaseState<PlayerState>
    {
        protected PlayerController ctx;

        protected BasePlayerState(PlayerController ctx, PlayerState key) : base(key)
        {
            this.ctx = ctx;
        }

        public override void EnterState()
        {
            ctx.StateTime = 0;
        }

        public override void UpdateState()
        {
            ctx.StateTime += Time.deltaTime;
        }

        public override void PhysicsUpdateState()
        {
            Vector3 xzVelocity = ctx.Anim.deltaPosition;
            float yVelocity = GetYVelocity();
            ctx.PlayerVelocity = new Vector3(xzVelocity.x, yVelocity, xzVelocity.z);
        }

        protected virtual float GetYVelocity()
        {
            if (!ctx.IsGrounded)
            {
                return ctx.PlayerVelocity.y - 9.81f * Time.fixedDeltaTime;
            }

            return Mathf.Max(0.0f, ctx.PlayerVelocity.y);
        }

        protected virtual void CalculatePlayerTransform()
        {
            ctx.SnapGroundForce = Vector3.zero;

            if (ctx.IsSnapGround && ctx.IsGrounded)
            {
                ctx.SnapGroundForce = Vector3.down;
            }

            ctx.PlayerCC.Move(ctx.PlayerVelocity * Time.deltaTime + ctx.SnapGroundForce);
        }
    }
}
