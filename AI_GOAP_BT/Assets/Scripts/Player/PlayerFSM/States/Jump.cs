using UnityEngine;

namespace Player.FSM
{
    public class Jump : BasePlayerState
    {
        public Jump(PlayerController ctx, PlayerState key) : base(ctx, key)
        {
        }

        public override void ExitState()
        {
        }

        public override PlayerState GetNextState()
        {
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
    }
}
