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
            ctx.MyBrain.Navigator.AI.enableRotation = true;
            ctx.RootRotation = false;
            ctx.Anim.CrossFadeInFixedTime(AnimHash.Strafe, 0.1f);
        }

        public override void ExitState()
        {

        }

        public override void UpdateState()
        {
            base.UpdateState();

            DecideStopDistance();
        }

        public override void PhysicsUpdateState()
        {
            float speed = ctx.MyBrain.Navigator.AI.desiredVelocity.magnitude;
            float normalized = Mathf.InverseLerp(0f, ctx.MyBrain.Navigator.AI.maxSpeed, speed);
            ctx.SetTargetAccel(normalized * 4f);

            Vector3 origin = ctx.transform.position + Vector3.up * 1.2f;
            Vector3 direction = ((ctx.MyBrain.Navigator.AI.endOfPath + Vector3.up * 1.2f) - origin).normalized;
            obstacle = Physics.RaycastNonAlloc(
                    ctx.transform.position + Vector3.up * 1.2f,
                    direction,
                    hits,
                    stoppingDistance,
                    WorldManager.Instance.GetLevelLayers()
                );
        }

        public override AnimState GetNextState()
        {
            if (ctx.MyBrain.Sensor.IsAlert || ctx.AttackedDirection != Vector3.zero)
                return AnimState.LookAtMove;
            if (Vector3.Distance(ctx.transform.position, ctx.MyBrain.Navigator.AI.endOfPath) <= stoppingDistance
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
            if (Vector3.Distance(ctx.MyBrain.Navigator.AI.steeringTarget, ctx.transform.position) <= 0.5f)
                return false;

            Vector3 vel = ctx.MyBrain.Navigator.AI.desiredVelocity;
            if (vel.sqrMagnitude < 0.001f) return false;

            Vector3 tgt = ctx.MyBrain.Navigator.AI.steeringTarget - ctx.transform.position;

            vel.y = 0f;
            tgt.y = 0f;

            vel.Normalize();
            tgt.Normalize();

            if (Vector3.Angle(ctx.transform.forward, tgt) <= 30f)
                return false;

            if (Vector3.Angle(vel, tgt) >= 150f)
                return true;
            return false;
        }
    }
}