using FSM;
using UnityEngine;

namespace CapturePoint
{
    public abstract class BaseCaptureState : BaseState<CaptureState>
    {
        protected CapturePoint ctx;

        protected BaseCaptureState(CapturePoint ctx, CaptureState key) : base(key)
        {
            this.ctx = ctx;
        }
        public override void EnterState()
        {
            ctx.StateTime = 0f;
        }

        public override void UpdateState()
        {
            ctx.StateTime += Time.deltaTime;
        }
    }
}
