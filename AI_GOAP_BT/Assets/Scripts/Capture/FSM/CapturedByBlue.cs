using UnityEngine;

namespace CapturePoint
{
    public class CapturedByBlue : BaseCaptureState
    {
        public CapturedByBlue(CapturePoint ctx, CaptureState key) : base(ctx, key)
        {
            this.ctx = ctx;
        }

        public override void EnterState()
        {
            ctx.UpdateDecalColor(1);
            base.EnterState();
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
            if (ctx.IsEmptyPlace())
            {
                ctx.CaptureGauge += ctx.CaptureAmount * 3 * Time.fixedDeltaTime;
                ctx.CaptureGauge = Mathf.Clamp(ctx.CaptureGauge, -100f, 100f);
            }
            else
            {
                float amountPerSecond = ctx.CaptureAmount * ctx.CaptureAmountThreshold();
                ctx.CaptureGauge += amountPerSecond * Time.fixedDeltaTime;
                ctx.CaptureGauge = Mathf.Clamp(ctx.CaptureGauge, -100f, 100f);
            }
        }

        public override CaptureState GetNextState()
        {
            if (ctx.CaptureGauge <= 0) return CaptureState.Neutral;
            return StateKey;
        }

        public override void OnTriggerEnter(Collider other)
        {
            ctx.AddIntruder(other.transform);
        }

        public override void OnTriggerExit(Collider other)
        {
            ctx.RemoveIntruder(other.transform);
        }

        public override void OnTriggerStay(Collider other)
        {

        }
    }
}
