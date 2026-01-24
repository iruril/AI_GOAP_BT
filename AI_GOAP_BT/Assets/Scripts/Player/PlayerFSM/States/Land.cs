using UnityEngine;

namespace Player.FSM
{
    public class Land : BasePlayerState
    {
        public Land(PlayerController ctx, PlayerState key) : base(ctx, key)
        {
        }

        public override void EnterState()
        {
            base.EnterState();
            ctx.Anim.applyRootMotion = true;
            ctx.IsOnJumping = false;
            ctx.Anim.CrossFadeInFixedTime(AnimHash.Land, 0.1f);
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
            if (ctx.StateTime > 0.3f)
            {
                if (ctx.Input.MoveInputMap == Vector2.zero)
                {
                    return PlayerState.Idle;
                }
                else
                {
                    return PlayerState.Move;
                }
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
    }
}
