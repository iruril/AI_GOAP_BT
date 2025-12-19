using UnityEngine;
using Mirror;

namespace Observer
{
    public class Observer : NetworkBehaviour
    {
        private CharacterController cc;

        [Header("Move")]
        [SerializeField] private float maxSpeed = 5f;
        [SerializeField] private float acceleration = 10f;

        [Header("Rotation")]
        [SerializeField] private float sensitivity = 10.0f;

        private float yaw = 0;
        private float pitch = 0;

        private Vector3 currentVelocity = Vector3.zero;

        private bool isChatting = false;

        private void Awake()
        {
            cc = GetComponent<CharacterController>();
        }

        public override void OnStartLocalPlayer()
        {
            GameManager.GetInstance().MyPlayer = this.gameObject;
            MainCamManager.Instance.SetCamTarget(this.transform);

            LockCursor(true);
        }

        public override void OnStopLocalPlayer()
        {
            GameManager.GetInstance().MyPlayer = null;
            MainCamManager.Instance.SetCamTarget(null);

            LockCursor(false);
        }

        void Update()
        {
            if (!isLocalPlayer) return;

            HandleChatToggle();

            if (isChatting)
                return;

            HandleRotation();
            HandleMovement();
        }

        private void HandleChatToggle()
        {
            if (isChatting)
                return;

            if (!GameManager.GetInstance().InputMap.ConsumeChatKeyDown())
                return;

            isChatting = true;
            EnterChatMode();
        }

        private void EnterChatMode()
        {
            LockCursor(false);

            ChatLog.Instance.InputField.gameObject.SetActive(true);
            ChatLog.Instance.InputField.ActivateInputField();
            ChatLog.Instance.InputField.Select();

            currentVelocity = Vector3.zero;
        }

        public void ForceExitChat()
        {
            if (!isChatting) return;

            isChatting = false;
            ExitChatMode();
        }

        private void ExitChatMode()
        {
            LockCursor(true);

            ChatLog.Instance.InputField.DeactivateInputField();
            ChatLog.Instance.InputField.gameObject.SetActive(false);
        }

        private void HandleRotation()
        {
            yaw += GameManager.GetInstance().InputMap.XRotationEuler * sensitivity;
            pitch += GameManager.GetInstance().InputMap.YRotationEuler * sensitivity;
            pitch = Mathf.Clamp(pitch, -75f, 75f);

            transform.rotation = Quaternion.Euler(-pitch, yaw, 0);
        }

        private void HandleMovement()
        {
            var input = GameManager.GetInstance().InputMap.MoveInputMap;

            Vector3 dir =
                transform.forward * input.y +
                transform.right * input.x;

            dir.Normalize();

            Vector3 targetVelocity = dir * maxSpeed;

            Vector3 diff = targetVelocity - currentVelocity;
            float maxDelta = acceleration * Time.deltaTime;

            Vector3 delta = Vector3.ClampMagnitude(diff, maxDelta);
            currentVelocity += delta;

            cc.Move(currentVelocity * Time.deltaTime);
        }

        private void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
