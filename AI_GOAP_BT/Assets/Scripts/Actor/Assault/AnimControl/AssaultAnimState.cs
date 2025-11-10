using UnityEngine;
using FSM;

namespace AnimControl.Assault
{
    public abstract class AssaultAnimState : BaseState<AnimState>
    {
        protected AssaultAnimFSM ctx;

        protected AssaultAnimState(AssaultAnimFSM ctx, AnimState key) : base(key)
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
