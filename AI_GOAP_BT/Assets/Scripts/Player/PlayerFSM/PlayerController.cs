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
        public CharacterController PlayerCC { get; private set; }
        public TPSCamController CamController { get; private set; }
        public GunHandler GunController { get; private set; }
        public Animator Anim { get; private set; }
        public PlayerIKHandler IKManager { get; private set; }
        public Stat Stat { get; private set; }

        public Vector3 PlayerVelocity { get; set; } = Vector3.zero;
        public Vector3 PlayerXZVelocity { get; set; } = Vector3.zero;
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
            PlayerCC = GetComponent<CharacterController>();
            GunController = GetComponent<GunHandler>();
            CamController = GetComponent<TPSCamController>();
            Anim = GetComponent<Animator>();
            IKManager = GetComponent<PlayerIKHandler>();
            Stat = GetComponent<Stat>();
            GroundChecker = GetComponent<GroundChecker>();

            PlayerCC.enabled = false;
            CamController.enabled = false;
            GroundChecker.enabled = false;

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
            PlayerCC.enabled = true;
            CamController.enabled = true;
            GroundChecker.enabled = true;

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
            if (!CanProcess()) return;

            base.Update();
            UpdateStandardNormals();
            UpdateXZProjectionVelocity();
            UpdateAccelation();
            UpdateCamYRotationDelta();
        }

        protected override void FixedUpdate()
        {
            if (!CanProcess()) return;

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

        private void UpdateCamYRotationDelta()
        {
            float currYaw = CamController.CamTarget.eulerAngles.y;
            DeltaYaw = Mathf.DeltaAngle(PrevYaw, currYaw);
            PrevYaw = currYaw;
        }

        float accelRef;
        void UpdateAccelation()
        {
            float targetAccel;
            if (Input.MoveInputMap == Vector2.zero)
                targetAccel = 0f;
            else
                targetAccel = Input.Run ? 4f : 2f;

            float currentAccel = Anim.GetFloat(AnimHash.Accelation);
            Anim.SetFloat(AnimHash.Accelation, Mathf.SmoothDamp(currentAccel, targetAccel, ref accelRef, 0.25f));
        }

        private void UpdateXZProjectionVelocity()
        {
            PlayerXZVelocity = new Vector3(PlayerCC.velocity.x, 0, PlayerCC.velocity.z);
        }

        private void UpdateStandardNormals()
        {
            float yRotation = MainCamManager.Instance.GetCameraRotaionY();

            if (!IsOnJumping)
            {
                PlayerForward = Quaternion.AngleAxis(yRotation, Vector3.up) * Vector3.forward;
                PlayerRight = Quaternion.AngleAxis(yRotation, Vector3.up) * Vector3.right;
            }
        }

        private void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
