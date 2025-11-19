using UnityEngine;

namespace AnimControl.Assault
{
    public class Move : BaseAssaultAnimState
    {
        private float stoppingDistance;
        private int obstacle;
        private RaycastHit[] hits = new RaycastHit[1];

        public Move(AssaultAnimFSM ctx, AnimState key) : base(ctx, key)
        {
            this.ctx = ctx;
        }

        public override void EnterState()
        {
            base.EnterState();
            ctx.Navigator.AI.enableRotation = true;
            ctx.RootRotation = false;
            ctx.Anim.CrossFade(AnimHash.Strafe, 0.15f);
        }

        public override void ExitState()
        {

        }

        public override void UpdateState()
        {
            base.UpdateState();

            float speed = ctx.Navigator.AI.velocity.magnitude;
            float normalized = Mathf.InverseLerp(0f, ctx.Navigator.AI.maxSpeed, speed);

            ctx.SetTargetAccel(normalized * 4f);

            DecideStopDistance();
        }

        public override void PhysicsUpdateState()
        {
            Vector3 origin = ctx.transform.position + Vector3.up * 1.4f;
            Vector3 direction = ((ctx.Navigator.AI.endOfPath + Vector3.up * 1.4f) - origin).normalized;
            obstacle = Physics.RaycastNonAlloc(
                    ctx.transform.position + Vector3.up * 1.4f,
                    direction,
                    hits,
                    stoppingDistance,
                    WorldManager.Instance.GetLevelLayers()
                );

            ctx.LookHitDirection();
        }

        public override AnimState GetNextState()
        {
            if (ctx.MySensor.HasTarget)
                return AnimState.LookAtMove;
            if (Vector3.Distance(ctx.transform.position, ctx.Navigator.AI.endOfPath) <= stoppingDistance
                && obstacle == 0)
                return AnimState.Stop;
            if (IsOnTurnOppsiteCondition())
                return AnimState.TurnOpposite;
            return StateKey;
        }

        public override void OnTriggerEnter(Collider other) { }

        public override void OnTriggerStay(Collider other) { }

        public override void OnTriggerExit(Collider other) { }

        void DecideStopDistance()
        {
            int snapAccel = Mathf.Clamp(Mathf.RoundToInt(ctx.Accel), 1, 4);
            switch (snapAccel)
            {
                case 3:
                    stoppingDistance = 3f;
                    break;
                case 4:
                    stoppingDistance = 4.5f;
                    break;
                default:
                    stoppingDistance = 0.7f;
                    break;
            }
        }

        bool IsOnTurnOppsiteCondition()
        {
            if (Vector3.Distance(ctx.Navigator.AI.steeringTarget, ctx.transform.position) <= 0.5f)
                return false;

            Vector3 vel = ctx.Navigator.AI.desiredVelocity;
            if (vel.sqrMagnitude < 0.001f) return false;

            Vector3 tgt = ctx.Navigator.AI.steeringTarget - ctx.transform.position;

            vel.y = 0f;
            tgt.y = 0f;

            vel.Normalize();
            tgt.Normalize();

            if (Vector3.Angle(vel, tgt) >= 150f)
                return true;
            return false;
        }
    }
}