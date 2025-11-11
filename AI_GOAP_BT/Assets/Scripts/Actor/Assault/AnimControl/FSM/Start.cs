using UnityEngine;

namespace AnimControl.Assault
{
    public class Start : AssaultAnimState
    {
        private float exitTime = 0;

        public Start(AssaultAnimFSM ctx, AnimState key) : base(ctx, key)
        {
            this.ctx = ctx;
        }

        public override void EnterState()
        {
            base.EnterState();
            ctx.DecideAccelInitial();

            switch (ctx.Accel)
            {
                case 1:
                    exitTime = 1.37f;
                    break;
                case 2:
                    exitTime = 1.37f;
                    break;
                case 3:
                    exitTime = 1.1f;
                    break;
                case 4:
                    exitTime = 0.9f;
                    break;
            }

            ctx.Navigator.AI.enableRotation = false;
            ctx.RootRotation = true;

            Vector3 desiredDir = ctx.Navigator.AI.steeringTarget - ctx.transform.position;
            desiredDir.y = 0f;
            desiredDir.Normalize();

            float angle = Vector3.Angle(ctx.transform.forward, desiredDir);
            float angleLerp = Mathf.InverseLerp(0f, 180f, angle);

            float turnDir = Mathf.Sign(Vector3.Cross(ctx.transform.forward, desiredDir).y); // -1 = ¿ÞÂÊ, +1 = ¿À¸¥ÂÊ  

            ctx.Anim.SetFloat(AnimHash.AngleLerp, angleLerp);

            if (turnDir > 0)
            {
                ctx.Anim.CrossFade(AnimHash.StartMove_R, 0.1f);
            }
            else
            {
                ctx.Anim.CrossFade(AnimHash.StartMove_L, 0.1f);
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
            if (ctx.StateTime >= exitTime) return AnimState.Move;
            return StateKey;
        }

        public override void OnTriggerEnter(Collider other) { }

        public override void OnTriggerStay(Collider other) { }

        public override void OnTriggerExit(Collider other) { }
    }
}