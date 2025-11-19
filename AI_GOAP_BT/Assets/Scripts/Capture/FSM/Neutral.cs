using UnityEngine;

namespace CapturePoint
{
    public class Neutral : BaseCaptureState
    {
        public Neutral(CapturePoint ctx, CaptureState key) : base(ctx, key)
        {
            this.ctx = ctx;
        }

        public override void EnterState()
        {
            ctx.UpdateDecalColor(0);
        }

        public override void ExitState()
        {

        }

        public override void UpdateState()
        {
        }

        public override void PhysicsUpdateState()
        {
            if (ctx.IsEmptyPlace())
            {
                if(ctx.CaptureGauge > 0)
                {
                    ctx.CaptureGauge -= ctx.CaptureAmount * 3 * Time.fixedDeltaTime;
                    ctx.CaptureGauge = Mathf.Clamp(ctx.CaptureGauge, -100f, 100f);
                }
                else if(ctx.CaptureGauge < 0)
                {
                    ctx.CaptureGauge += ctx.CaptureAmount * 3 * Time.fixedDeltaTime;
                    ctx.CaptureGauge = Mathf.Clamp(ctx.CaptureGauge, -100f, 100f);
                }
                else
                {
                    ctx.CaptureGauge = 0f;
                }
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
            if (ctx.CaptureGauge >= 100f) return CaptureState.CapturedByBlue;
            if (ctx.CaptureGauge <= -100f) return CaptureState.CapturedByRed;
            return StateKey;
        }

        public override void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<Stat>(out var stat))
            {
                stat.CurrentCapture = ctx;
                ctx.AddIntruder(stat);
            }
        }

        public override void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<Stat>(out var stat))
            {
                stat.CurrentCapture = null;
                ctx.RemoveIntruder(stat);
            }
        }

        public override void OnTriggerStay(Collider other)
        {

        }
    }
}
