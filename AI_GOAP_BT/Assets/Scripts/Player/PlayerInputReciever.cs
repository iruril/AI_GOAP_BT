using Mirror;
using UnityEngine;
using Player.Input;

namespace Player.FSM
{
    public class PlayerInputReciever : NetworkBehaviour
    {
        public InputRecorder Input { get; private set; }

        public override void OnStartLocalPlayer()
        {
            Input = GetComponent<InputRecorder>();
        }
    }
}
