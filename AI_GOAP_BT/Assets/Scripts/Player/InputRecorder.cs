using UnityEngine;
using UnityEngine.InputSystem;
using MEC;
using System.Collections.Generic;

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
        public bool IsOnMoveInput { get; private set; } = false;

        public float HorizontalInput { get; private set; }
        public float VerticalInput { get; private set; }
        public float RawHorizontalInput { get; private set; }
        public float RawVerticalInput { get; private set; }

        private Vector2 _smoothInputMap = Vector3.zero;
        private Vector2 _currentInputMap = Vector3.zero;
        public Vector2 CurrentInputMap { get; private set; }
        private const float _smoothTimeOnGround = 0.15f;

        private CoroutineHandle resetCoroutine;

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
            Vector2 value = context.ReadValue<Vector2>();

            if (value != Vector2.zero)
            {
                IsOnMoveInput = true;
                Timing.KillCoroutines(resetCoroutine);
            }
            else
            {
                IsOnMoveInput = false;
                if (resetCoroutine == null)
                {
                    resetCoroutine = Timing.RunCoroutine(ResetMoveInputMap());
                }
            }

            if (IsOnMoveInput)
            {
                MoveInputMap = context.ReadValue<Vector2>();
            }
        }

        public void OnChatKey(InputAction.CallbackContext context)
        {
            ChatKeyDown = context.performed;
        }

        public void OnJumpInput(InputAction.CallbackContext context)
        {
            Jump = context.performed;
        }

        private IEnumerator<float> ResetMoveInputMap()
        {
            yield return Timing.WaitForSeconds(0.2f);
            MoveInputMap = Vector2.zero;
        }

        public bool ConsumeChatKeyDown()
        {
            if (!ChatKeyDown) return false;
            ChatKeyDown = false;
            return true;
        }

        private void InputMapCompensate()
        {
            Vector2 _moveInputMap = GameManager.GetInstance().InputMap.MoveInputMap;
            _currentInputMap = Vector2.SmoothDamp(_currentInputMap, _moveInputMap, ref _smoothInputMap, _smoothTimeOnGround);

            VerticalInput = _currentInputMap.y;
            HorizontalInput = _currentInputMap.x;
            Vector2 rawInput = _moveInputMap.normalized;
            RawHorizontalInput = rawInput.x;
            RawVerticalInput = rawInput.y;
        }
    }
}
