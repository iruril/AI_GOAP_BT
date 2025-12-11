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

        private void Awake()
        {
            cc = GetComponent<CharacterController>();
        }

        public override void OnStartLocalPlayer()
        {
            GameManager.GetInstance().MyPlayer = this.gameObject;
            MainCamManager.Instance.SetCamTarget(this.transform);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public override void OnStopLocalPlayer()
        {
            GameManager.GetInstance().MyPlayer = null;
            MainCamManager.Instance.SetCamTarget(null);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Update()
        {
            if (!isLocalPlayer) return;

            HandleRotation();
            HandleMovement();
        }

        private void HandleRotation()
        {
            yaw += GameManager.GetInstance().InputMap.CamInputMap.x * sensitivity;
            pitch += GameManager.GetInstance().InputMap.CamInputMap.y * sensitivity;
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
    }
}
