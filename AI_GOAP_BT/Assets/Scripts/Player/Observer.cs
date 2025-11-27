using UnityEngine;

namespace Observer
{
    using Player.Input;

    public class Observer : MonoBehaviour
    {
        private InputRecorder inputMap;
        private Rigidbody rb;

        [Header("Move")]
        [SerializeField] private float maxSpeed = 5f;
        [SerializeField] private float acceleration = 10f;

        [Header("Rotation")]
        [SerializeField] private float sensitivity = 10.0f;

        private float yaw;
        private float pitch;

        private Vector3 currentVelocity = Vector3.zero;

        private void Awake()
        {
            inputMap = GetComponent<InputRecorder>();
            rb = GetComponent<Rigidbody>();

            pitch = transform.rotation.eulerAngles.y;
            yaw = transform.rotation.eulerAngles.x;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            yaw = yaw + inputMap.CamInputMap.x * sensitivity * Time.deltaTime;
            pitch = pitch + inputMap.CamInputMap.y * sensitivity * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, -75, 75);

            transform.rotation = Quaternion.Euler(-pitch, yaw, 0);
            rb.linearVelocity = currentVelocity;
        }

        private void FixedUpdate()
        {
            Vector3 dir =
                transform.forward * inputMap.MoveInputMap.y +
                transform.right * inputMap.MoveInputMap.x;

            dir.Normalize();

            Vector3 targetVelocity = dir * maxSpeed;

            currentVelocity = Vector3.MoveTowards(
                currentVelocity,
                targetVelocity,
                acceleration * Time.fixedDeltaTime
            );
        }
    }
}
