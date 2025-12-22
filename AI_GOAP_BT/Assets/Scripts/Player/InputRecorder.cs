using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Input
{
    public class InputRecorder : MonoBehaviour
    {
        [SerializeField][Range(0, 100)] private float _rotateSpeedOnMouse = 10.0f;
        [SerializeField][Range(0, 100)] private float _rotateSpeedOnGamepad = 40.0f;

        public float SensitivityOnMouse
        {
            get
            {
                return _rotateSpeedOnMouse;
            }
            set
            {
                _rotateSpeedOnMouse = value;
            }
        }

        public float SensitivityOnGamepad
        {
            get
            {
                return _rotateSpeedOnGamepad;
            }
            set
            {
                _rotateSpeedOnGamepad = value;
            }
        }

        public float YRotationEuler { get; private set; } = 0;
        public float XRotationEuler { get; private set; } = 0;
        public Vector2 MoveInputMap { get; private set; }
        public bool ChatKeyDown { get; private set; }
        public bool Jump { get; set; } = false;
        public bool Aim { get; set; } = false;
        public bool Run { get; set; } = false;
        public bool Trigger { get; set; } = false;

        public float HorizontalInput { get; private set; }
        public float VerticalInput { get; private set; }
        public float RawHorizontalInput { get; private set; }
        public float RawVerticalInput { get; private set; }

        private Vector2 _smoothInputMap = Vector3.zero;
        public Vector2 CurrentInputMap { get; private set; }
        private const float _smoothTimeOnGround = 0.15f;

        private void Update()
        {
            InputMapCompensate();
        }

        public void OnCamInput(InputAction.CallbackContext context)
        {
            Vector2 value = context.ReadValue<Vector2>();
            if (context.control.device is Mouse)
            {
                float sensitivity = _rotateSpeedOnMouse;

                XRotationEuler = -value.y * sensitivity;
                YRotationEuler = value.x * sensitivity;
            }
            else
            {
                float sensitivity = _rotateSpeedOnGamepad * Time.deltaTime;

                XRotationEuler = -value.y * sensitivity;
                YRotationEuler = value.x * sensitivity;
            }
        }

        public void OnMoveInput(InputAction.CallbackContext context)
        {
            MoveInputMap = context.ReadValue<Vector2>();
        }

        public void OnChatKey(InputAction.CallbackContext context)
        {
            ChatKeyDown = context.performed;
        }

        public void OnJumpInput(InputAction.CallbackContext context)
        {
            Jump = context.performed;
        }

        public void OnAimInput(InputAction.CallbackContext context)
        {
            Aim = context.performed;
        }
        public void OnRunInput(InputAction.CallbackContext context)
        {
            Run = context.performed;
        }

        public void OnTrrigerInput(InputAction.CallbackContext context)
        {
            Trigger = context.performed;
        }

        public bool ConsumeChatKeyDown()
        {
            if (!ChatKeyDown) return false;
            ChatKeyDown = false;
            return true;
        }

        private void InputMapCompensate()
        {
            CurrentInputMap = Vector2.SmoothDamp(CurrentInputMap, MoveInputMap, ref _smoothInputMap, _smoothTimeOnGround);

            VerticalInput = CurrentInputMap.y;
            HorizontalInput = CurrentInputMap.x;
            RawHorizontalInput = MoveInputMap.x;
            RawVerticalInput = MoveInputMap.y;
        }
    }
}
