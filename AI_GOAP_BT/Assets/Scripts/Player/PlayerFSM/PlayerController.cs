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
        public PlayerState State { get { return CurrentState.StateKey; } }

        public InputRecorder Input { get; private set; }
        public GroundChecker GroundChecker { get; private set; }
        public CharacterController PlayerCC { get; private set; }
        public TPSCamController CamController { get; private set; }
        public GunHandler GunController { get; private set; }
        public Animator Anim { get; private set; }
        public PlayerIKHandler IKManager { get; private set; }
        public CorpseGenerator CorpseSpawner { get; private set; }
        public Stat MyStat { get; private set; }

        [Header("점프 최대 높이")]
        [SerializeField] private float maxJumpHeight = 1f;

        public float JumpImpulseVelocity { get; private set; }
        public float PlayerGravityForce { get; private set; }

        public Vector3 PlayerVelocity { get; set; } = Vector3.zero;
        public Vector3 PlayerXZVelocity { get; set; } = Vector3.zero;
        public Vector3 SnapGroundForce { get; set; } = Vector3.zero;
        public Vector3 PlayerForward { get; set; }
        public Vector3 PlayerRight { get; set; }
        public Quaternion PlayerRotation { get; set; }
        public bool IsOnJumping { get; set; }
        public bool IsSnapGround => GroundChecker.IsSnapGround;
        public bool IsGrounded => GroundChecker.IsGrounded;

        public float OnAirSpeed { get; private set; }

        void Awake()
        {
            PlayerCC = GetComponent<CharacterController>();
            GunController = GetComponent<GunHandler>();
            CamController = GetComponent<TPSCamController>();
            Anim = GetComponent<Animator>();
            IKManager = GetComponent<PlayerIKHandler>();
            MyStat = GetComponent<Stat>();
            GroundChecker = GetComponent<GroundChecker>();
            CorpseSpawner = GetComponent<CorpseGenerator>();

            PlayerCC.enabled = false;
            CamController.enabled = false;
            GroundChecker.enabled = false;
            Anim.applyRootMotion = false;

            PlayerForward = transform.forward;
            PlayerRight = transform.right;
            PlayerRotation = transform.rotation;

            CalculateJumpVelocity();
        }

        public override void OnStartServer()
        {
            GunController.LoadGun("MPX");
            //LoadGun("AK-12");
            return;
        }

        public override void OnStartClient()
        {
            MyStat.OnDead += CorpseSpawner.SpawnCorpse;
            MyStat.OnRevive += CorpseSpawner.DespawnCorpse;
        }

        public override void OnStopClient()
        {
            MyStat.OnDead -= CorpseSpawner.SpawnCorpse;
            MyStat.OnRevive -= CorpseSpawner.DespawnCorpse;
        }

        public override void OnStartLocalPlayer()
        {
            PlayerCC.enabled = true;
            CamController.enabled = true;
            GroundChecker.enabled = true;
            Anim.applyRootMotion = true;

            InitStates();
            base.OnStartLocalPlayer();

            Input = GameManager.GetInstance().InputMap;
            GameManager.GetInstance().MyPlayer = this.gameObject;
            CamController.InitCam();

            GameManager.GetInstance().InputMap.LockCursor(true);
        }

        public override void OnStopLocalPlayer()
        {
            GameManager.GetInstance().MyPlayer = null;

            GameManager.GetInstance().InputMap.LockCursor(false);
        }

        protected override void Update()
        {
            if (!CanProcess()) return;

            base.Update();

            UpdateStandardNormals();
            UpdateXZProjectionVelocity();
            UpdateAccelation();
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

        private void CalculateJumpVelocity()
        {
            JumpImpulseVelocity = Mathf.Sqrt(2f * 9.81f * maxJumpHeight);
        }

        float accelRef;
        void UpdateAccelation()
        {
            float targetAccel;
            if (Input.MoveInputMap == Vector2.zero || GameManager.GetInstance().InputMap.IsOnStaticUI)
                targetAccel = 0f;
            else
                targetAccel = Input.Run && !GunController.OnReload ? 4f : 2f;

            float currentAccel = Anim.GetFloat(AnimHash.Accelation);
            Anim.SetFloat(AnimHash.Accelation, Mathf.SmoothDamp(currentAccel, targetAccel, ref accelRef, 0.25f));
        }

        private void UpdateXZProjectionVelocity()
        {
            PlayerXZVelocity = new Vector3(PlayerCC.velocity.x, 0, PlayerCC.velocity.z);
        }

        private void UpdateStandardNormals()
        {
            if (IsOnJumping || GameManager.GetInstance().InputMap.IsOnStaticUI)
            {
                return;
            }

            float yRotation = MainCamManager.Instance.GetCameraRotaionY();

            PlayerForward = Quaternion.AngleAxis(yRotation, Vector3.up) * Vector3.forward;
            PlayerRight = Quaternion.AngleAxis(yRotation, Vector3.up) * Vector3.right;
        }


        public void CalculateOnAirSpeed()
        {
            Vector2 xzVelocity = new Vector2(PlayerCC.velocity.x, PlayerCC.velocity.z);
            float currentSpeed = xzVelocity.magnitude;
            OnAirSpeed = Mathf.Lerp(0f, 6.5f, currentSpeed / 6.5f);
        }
    }
}
