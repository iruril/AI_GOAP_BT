using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Input
{
    public class InputRecorder : MonoBehaviour
    {
        public Vector2 CamInputMap { get; private set; }
        public Vector2 MoveInputMap { get; private set; }

        public void OnCamInput(InputAction.CallbackContext context)
        {
            Vector2 value = context.ReadValue<Vector2>();
            CamInputMap = value;
        }

        public void OnMoveInput(InputAction.CallbackContext context)
        {
            Vector2 value = context.ReadValue<Vector2>(); 
            MoveInputMap = value;
        }
    }
}
