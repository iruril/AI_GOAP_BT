using FSM;
using RootMotion.FinalIK;
using UnityEngine;

namespace Player.FSM
{
    public enum PlayerState
    {
        Idle,
        Start,
        Walk,
        Run,
        Aim,
        Stop,
        Jump,
        Fall,
        Land
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

        public override void OnStartServer()
        {
            InitStates();
            base.OnStartServer();
        }

        protected override void Update()
        {
            base.Update();
            PlayerVectorHandler();
        }

        private void InitStates()
        {
            States.Add(PlayerState.Idle, new Idle(this, PlayerState.Idle));
            States.Add(PlayerState.Start, new Start(this, PlayerState.Start));
            States.Add(PlayerState.Walk, new Walk(this, PlayerState.Walk));
            States.Add(PlayerState.Run, new Run(this, PlayerState.Run));
            States.Add(PlayerState.Aim, new Aim(this, PlayerState.Aim));
            States.Add(PlayerState.Stop, new Stop(this, PlayerState.Stop));
            States.Add(PlayerState.Jump, new Jump(this, PlayerState.Jump));
            States.Add(PlayerState.Fall, new Fall(this, PlayerState.Fall));
            States.Add(PlayerState.Land, new Land(this, PlayerState.Land));

            CurrentState = States[PlayerState.Idle];
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
