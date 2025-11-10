using UnityEngine;

namespace AnimControl.Assault
{
    public class Start : AssaultAnimState
    {
        private const float _exitTime = 0.93f;

        public Start(AssaultAnimFSM ctx, AnimState key) : base(ctx, key)
        {
            this.ctx = ctx;
        }

        public override void EnterState()
        {
            base.EnterState();
            ctx.DecideAccelInitial();

            ctx.Navigator.AI.enableRotation = false;
            Vector3 desiredDir = ctx.Navigator.AI.steeringTarget - ctx.transform.position;
            desiredDir.y = 0f;
            desiredDir.Normalize();

            float angle = Vector3.Angle(ctx.transform.forward, desiredDir);
            float angleLerp = Mathf.InverseLerp(0f, 180f, angle);

            float turnDir = Mathf.Sign(Vector3.Cross(ctx.transform.forward, desiredDir).y); // -1 = ¿ÞÂÊ, +1 = ¿À¸¥ÂÊ  

            ctx.Anim.SetFloat(AnimHash.AngleLerp, angleLerp);

            if (turnDir > 0)
            {
                ctx.Anim.CrossFade(AnimHash.StartMove_R, 0.15f);
            }
            else
            {
                ctx.Anim.CrossFade(AnimHash.StartMove_L, 0.15f);
            }
        }

        public override void ExitState()
        {

        }

        public override void UpdateState()
        {
            base.UpdateState();
        }

        public override void PhysicsUpdateState()
        {

        }

        public override AnimState GetNextState()
        {
            if (ctx.StateTime >= _exitTime) return AnimState.Move;
            return StateKey;
        }

        public override void OnTriggerEnter(Collider other) { }

        public override void OnTriggerStay(Collider other) { }

        public override void OnTriggerExit(Collider other) { }
    }
}