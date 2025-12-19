using FSM;
using RootMotion.FinalIK;
using UnityEngine;

namespace Player.FSM
{
    public enum PlayerState
    {
        Idle,
        Walk,
        Run,
        ToIdle,
        Jump,
        Fall,
        Land,
    }

    public class PlayerController : StateManager<PlayerState>
    {
        public float StateTime { get; set; }
        public PlayerInputReciever InputMap { get; private set; }
        public GroundChecker GroundChecker { get; private set; }
        public CharacterController PlayerCC { get; private set; }
        public TPSCamController CamController { get; private set; }
        public Animator Anim { get; private set; }
        public Stat Stat { get; private set; }

        public Vector3 PlayerVelocity { get; set; } = Vector3.zero;
        public float PlayerCurrentSpeed { get; set; }
        public Vector3 SnapGroundForce { get; set; } = Vector3.zero;
        public Vector3 PlayerForward { get; set; }
        public Vector3 PlayerRight { get; set; }
        public Quaternion PlayerRotation { get; set; }
        public bool IsOnJumping { get; set; }
        public bool IsSnapGround => GroundChecker.IsSnapGround;
        public bool IsGrounded => GroundChecker.IsGrounded;

        void Awake()
        {
            InputMap = GetComponent<PlayerInputReciever>();
            PlayerCC = GetComponent<CharacterController>();
            CamController = GetComponent<TPSCamController>();
            Anim = GetComponent<Animator>();
            Stat = GetComponent<Stat>();
            GroundChecker = GetComponent<GroundChecker>();

            PlayerForward = transform.forward;
            PlayerRight = transform.right;
            PlayerRotation = transform.rotation;
        }

        protected override void Update()
        {
            base.Update();
            PlayerVectorHandler();
        }

        private void PlayerVectorHandler()
        {
            float yRotation = CamController.GetCameraRotaionY();

            if (!IsOnJumping)
            {
                PlayerForward = Quaternion.AngleAxis(yRotation, Vector3.up) * Vector3.forward;
                PlayerRight = Quaternion.AngleAxis(yRotation, Vector3.up) * Vector3.right;
            }
        }
    }
}
