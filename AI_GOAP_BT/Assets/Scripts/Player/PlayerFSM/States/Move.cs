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
            ctx.IsOnJumping = false;
            ctx.Anim.CrossFadeInFixedTime(AnimHash.Strafe, 0.25f);
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
            base.PhysicsUpdateState();
        }

        public override PlayerState GetNextState()
        {
            if (ctx.Input.Jump && !GameManager.GetInstance().InputMap.IsOnStaticUI) return PlayerState.Jump;
            if (!ctx.IsGrounded) return PlayerState.Fall;
            if (ctx.Input.Crouch) return PlayerState.Crouch;
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
            if (Vector2.Angle(ctx.Input.MoveInputMap, Vector2.up) > 45f)
            {
                return false;
            }

            float camYaw = ctx.CamController.YRotation;
            float playerYaw = ctx.transform.eulerAngles.y;

            float delta = Mathf.Abs(Mathf.DeltaAngle(playerYaw, camYaw));
            return delta >= 135f;
        }
    }
}
