using FSM;
using Player.Input;
using UnityEngine;

namespace Player.FSM
{
    public enum PlayerState
    {
        Idle,
        Move,
        TurnOpposite,
        Jump,
        Fall,
        Land
    }

    public class PlayerController : StateManager<PlayerState>
    {
        public float StateTime { get; set; }
        public InputRecorder Input { get; private set; }
        public GroundChecker GroundChecker { get; private set; }
        public Rigidbody Rb { get; private set; }
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

        public float PrevYaw { get; private set; }
        public float DeltaYaw { get; private set; }

        void Awake()
        {
            Rb = GetComponent<Rigidbody>();
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
            return;
        }

        public override void OnStartLocalPlayer()
        {
            InitStates();
            base.OnStartLocalPlayer();

            Input = GameManager.GetInstance().InputMap;
            GameManager.GetInstance().MyPlayer = this.gameObject;
            CamController.InitCam();
            PrevYaw = CamController.CamTarget.eulerAngles.y;

            LockCursor(true);
        }

        public override void OnStopLocalPlayer()
        {
            GameManager.GetInstance().MyPlayer = null;

            LockCursor(false);
        }

        protected override void Update()
        {
            if (ServerOnly)
            {
                if (!isServer) return;
            }
            else
            {
                if (!isLocalPlayer) return;
            }
            base.Update();
            PlayerVectorHandler();
        }

        protected override void FixedUpdate()
        {
            if (ServerOnly)
            {
                if (!isServer) return;
            }
            else
            {
                if (!isLocalPlayer) return;
            }
            base.FixedUpdate();
        }

        private void InitStates()
        {
            States.Add(PlayerState.Idle, new Idle(this, PlayerState.Idle));
            States.Add(PlayerState.Move, new Move(this, PlayerState.Move));
            States.Add(PlayerState.TurnOpposite, new TurnOpposite(this, PlayerState.TurnOpposite));
            States.Add(PlayerState.Jump, new Jump(this, PlayerState.Jump));
            States.Add(PlayerState.Fall, new Fall(this, PlayerState.Fall));
            States.Add(PlayerState.Land, new Land(this, PlayerState.Land));

            CurrentState = States[PlayerState.Idle];
        }

        float accelRef;
        private void PlayerVectorHandler()
        {
            float yRotation = MainCamManager.Instance.GetCameraRotaionY();

            if (!IsOnJumping)
            {
                PlayerForward = Quaternion.AngleAxis(yRotation, Vector3.up) * Vector3.forward;
                PlayerRight = Quaternion.AngleAxis(yRotation, Vector3.up) * Vector3.right;
            }

            float targetAccel;
            if (Input.MoveInputMap == Vector2.zero)
                targetAccel = 0f;
            else
                targetAccel = Input.Run ? 4f : 2f;

            float currentAccel = Anim.GetFloat(AnimHash.Accelation);
            Anim.SetFloat(AnimHash.Accelation, Mathf.SmoothDamp(currentAccel, targetAccel, ref accelRef, 0.25f));

            float currYaw = CamController.CamTarget.eulerAngles.y;
            DeltaYaw = Mathf.DeltaAngle(PrevYaw, currYaw);
            PrevYaw = currYaw;
        }

        private void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
