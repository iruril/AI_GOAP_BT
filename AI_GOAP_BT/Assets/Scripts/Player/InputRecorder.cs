using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Input
{
    public enum UIState
    {
        None,
        Chat,
        Settings,
        Scoreboard
    }

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
        public bool Chat { get; private set; }
        public bool Jump { get; set; } = false;
        public bool Aim { get; set; } = false;
        public bool Run { get; set; } = false;
        public bool Trigger { get; set; } = false;
        public bool Reload { get; set; } = false;
        public bool ESC { get; set; } = false;
        public bool Tab { get; set; } = false;

        public UIState CurrentUIState { get; set; } = UIState.None;
        public bool IsOnStaticUI => CurrentUIState != UIState.None && CurrentUIState != UIState.Scoreboard;
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
            HandleESC();
            HandleChat();
            HandleScoreboard();
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
            Chat = context.performed;
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

        public void OnReloadInput(InputAction.CallbackContext context)
        {
            Reload = context.performed;
        }

        public void OnESCInput(InputAction.CallbackContext context)
        {
            ESC = context.performed;
        }

        public void OnTabInput(InputAction.CallbackContext context)
        {
            Tab = context.performed;
        }

        public bool ConsumeChatKeyDown()
        {
            if (!Chat) return false;
            Chat = false;
            return true;
        }

        public bool ConsumeESC()
        {
            if (!ESC) return false;
            ESC = false;
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

        private void HandleESC()
        {
            if (!ConsumeESC())
                return;

            switch (CurrentUIState)
            {
                case UIState.Chat:
                    ExitChat();
                    break;

                case UIState.Settings:
                    ExitSetting();
                    break;

                case UIState.Scoreboard:
                    ExitScoreboard();
                    break;

                case UIState.None:
                    EnterSetting();
                    break;
            }
        }

        private void HandleChat()
        {
            if (IsOnStaticUI || CurrentUIState == UIState.Chat)
                return;

            if (!ConsumeChatKeyDown())
                return;

            EnterChat();
        }


        public void EnterChat()
        {
            if (ChatLog.Instance == null) return;
            if (InGameUI.Instance != null)
            {
                if (InGameUI.Instance.GameWinHUD.gameObject.activeSelf ||
                   InGameUI.Instance.GameLoseHUD.gameObject.activeSelf) return;
            }

            CurrentUIState = UIState.Chat;
            LockCursor(false);

            ChatLog.Instance?.InputField.gameObject.SetActive(true);
            ChatLog.Instance?.InputField.ActivateInputField();
            ChatLog.Instance?.InputField.Select();
        }

        public void ExitChat()
        {
            if (ChatLog.Instance == null) return;

            Chat = false;

            RestoreCursor();
            ChatLog.Instance?.InputField.DeactivateInputField();
            ChatLog.Instance?.InputField.gameObject.SetActive(false);
            CurrentUIState = UIState.None;
        }

        public void EnterSetting()
        {
            if (SettingsPanel.Instance == null) return;
            if (InGameUI.Instance != null)
            {
                if (InGameUI.Instance.GameWinHUD.gameObject.activeSelf ||
                   InGameUI.Instance.GameLoseHUD.gameObject.activeSelf) return;
            }

            CurrentUIState = UIState.Settings;
            LockCursor(false);

            SettingsPanel.Instance.OpenSettings();
        }

        public void ExitSetting()
        {
            if (SettingsPanel.Instance == null) return;

            RestoreCursor();
            SettingsPanel.Instance.CloseSettings();
            CurrentUIState = UIState.None;
        }

        private void HandleScoreboard()
        {
            if (InGameUI.Instance == null) return;

            if (CurrentUIState != UIState.None &&
                CurrentUIState != UIState.Scoreboard)
                return;

            if (Tab)
            {
                if (CurrentUIState != UIState.Scoreboard)
                    EnterScoreboard();
            }
            else
            {
                if (CurrentUIState == UIState.Scoreboard)
                    ExitScoreboard();
            }
        }

        private void EnterScoreboard()
        {
            if (InGameUI.Instance == null) return;
            if (InGameUI.Instance.GameWinHUD.gameObject.activeSelf ||
               InGameUI.Instance.GameLoseHUD.gameObject.activeSelf) return;

            CurrentUIState = UIState.Scoreboard;
            InGameUI.Instance.ShowScoreboardHUD();
        }

        private void ExitScoreboard()
        {
            if (InGameUI.Instance == null) return;

            CurrentUIState = UIState.None; 
            InGameUI.Instance.HideScoreboardHUD();
        }

        private void RestoreCursor()
        {
            if (GameManager.GetInstance().IsGameplayScene)
                LockCursor(true);
            else
                LockCursor(false);
        }

        public void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
