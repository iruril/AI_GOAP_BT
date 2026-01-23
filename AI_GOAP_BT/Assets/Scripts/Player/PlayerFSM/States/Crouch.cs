using UnityEngine;

namespace Player.FSM
{
    public class Crouch : BasePlayerState
    {
        public Crouch(PlayerController ctx, PlayerState key) : base(ctx, key)
        {
        }

        public override void EnterState()
        {
            base.EnterState();
            ctx.Anim.applyRootMotion = true;
            ctx.Anim.CrossFadeInFixedTime(AnimHash.Crouch, 0.25f);
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
            if (!ctx.IsGrounded) return PlayerState.Fall;
            if (!ctx.Input.Crouch)
            {
                if (ctx.Input.MoveInputMap == Vector2.zero) 
                    return PlayerState.Idle;
                else 
                    return PlayerState.Move;
            }
            if (ctx.Input.MoveInputMap == Vector2.zero)
                return PlayerState.CrouchIdle;
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
    }
}
