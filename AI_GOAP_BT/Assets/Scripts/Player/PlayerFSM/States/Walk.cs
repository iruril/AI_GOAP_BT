using UnityEngine;

namespace Player.FSM
{
    public class Walk : BasePlayerState
    {
        public Walk(PlayerController ctx, PlayerState key) : base(ctx, key)
        {
        }

        public override void ExitState()
        {
            throw new System.NotImplementedException();
        }

        public override PlayerState GetNextState()
        {
            throw new System.NotImplementedException();
        }

        public override void OnTriggerEnter(Collider other)
        {
            throw new System.NotImplementedException();
        }

        public override void OnTriggerExit(Collider other)
        {
            throw new System.NotImplementedException();
        }

        public override void OnTriggerStay(Collider other)
        {
            throw new System.NotImplementedException();
        }
    }
}
