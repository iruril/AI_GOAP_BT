using UnityEngine;
using FSM;
using RootMotion.FinalIK;
using Mirror;
using Player.FSM;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace AnimControl.Assault
{
    public enum AnimState
    {
        Idle,
        Start,
        Move,
        Stop,
        TurnOpposite,
        LookAtMove
    }

    public class AssaultAnimFSM : StateManager<AnimState>
    {
        public GOAP.Assualt.AssaultBrain MyBrain { get; private set; }
        private AssaultAnimFSM _context => this;
        public Animator Anim { get; private set; }
        public Rigidbody MyRigid { get; private set; }
        public CapsuleCollider MyCol { get; private set; }
        public FullBodyBipedIK FBBIK { get; private set; }
        public AimIK AimIK { get; private set; }

        public AnimState CurrentStateKey;

        public float Accel { get; private set; } = 0f;
        public float TargetAccel { get; private set; } = 0f;
        public float StateTime { get; set; }
        public bool RootRotation = false;

        [SyncVar] public bool IsAimable;

        public Vector3 AttackedDirection { get; set; } = Vector3.zero;

        void Awake()
        {
            MyBrain = GetComponent<GOAP.Assualt.AssaultBrain>();
            Anim = GetComponent<Animator>();
            MyRigid = GetComponent<Rigidbody>();
            MyCol = GetComponent<CapsuleCollider>();
            FBBIK = GetComponent<FullBodyBipedIK>();
            AimIK = GetComponent<AimIK>();

            InitializeStates();
            CurrentState = States[AnimState.Idle];
        }

        private void Start()
        {
            IgnoreExistingPlayers();
            PlayerController.OnPlayerSpawned += IgnorePlayer;
        }

        private void OnDestroy()
        {
            PlayerController.OnPlayerSpawned -= IgnorePlayer;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            MyBrain.Sensor.MyStat.OnGrazeBullet += SetAttackedDirection;
            MyBrain.Sensor.MyStat.OnDead += OnDead;
            MyBrain.Navigator.OnSetDestination += DecideAccelByDistance;
        }

        public override void OnStopServer()
        {
            MyBrain.Sensor.MyStat.OnGrazeBullet -= SetAttackedDirection;
            MyBrain.Sensor.MyStat.OnDead -= OnDead;
            MyBrain.Navigator.OnSetDestination -= DecideAccelByDistance;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
        }

        private void IgnoreExistingPlayers()
        {
            var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                IgnorePlayer(player);
            }
        }

        private void IgnorePlayer(PlayerController player)
        {
            if (player == null) return;

            Collider playerCol = player.GetComponent<Collider>();
            if (playerCol != null && MyCol != null)
            {
                Physics.IgnoreCollision(MyCol, playerCol, true);
            }
        }

        void OnAnimatorMove()
        {
            if (!isServer || MyBrain.Sensor.MyStat.IsDead || Time.deltaTime <= 0) return;

            Vector3 nextPosition;
            Quaternion nextRotation;
            MyBrain.Navigator.AI.MovementUpdate(Time.fixedDeltaTime, out nextPosition, out nextRotation);

            Vector3 rootPosition = new Vector3(Anim.rootPosition.x, nextPosition.y, Anim.rootPosition.z);

            if (!MyBrain.Navigator.AI.enableRotation)
            {
                MyBrain.Navigator.AI.FinalizeMovement(rootPosition, MyRigid.rotation);

                if (RootRotation)
                {
                    MyRigid.MoveRotation(MyRigid.rotation * Anim.deltaRotation);
                }
            }
            else
            {
                MyBrain.Navigator.AI.FinalizeMovement(rootPosition, nextRotation);
            }
        }

        protected override void Update()
        {
            if (!isServer) return;

            if (MyBrain.Sensor.MyStat.IsDead) return;
            base.Update();
            CurrentStateKey = CurrentState.StateKey; 
            UpdateAimWeight();
            IsAimable = Aimable();
        }

        protected override void FixedUpdate()
        {
            if (!isServer) return;

            if (MyBrain.Sensor.MyStat.IsDead) return;
            base.FixedUpdate();
            UpdateMoveAxis();
            UpdateAcceleration();
            HandleAttackedDirection();
        }

        private void InitializeStates()
        {
            States.Add(AnimState.Idle, new Idle(_context, AnimState.Idle));
            States.Add(AnimState.Start, new Start(_context, AnimState.Start));
            States.Add(AnimState.Move, new Move(_context, AnimState.Move));
            States.Add(AnimState.LookAtMove, new LookAtMove(_context, AnimState.LookAtMove));
            States.Add(AnimState.Stop, new Stop(_context, AnimState.Stop));
            States.Add(AnimState.TurnOpposite, new TurnOpposite(_context, AnimState.TurnOpposite));
        }

        void UpdateMoveAxis()
        {
            Anim.SetFloat(AnimHash.XAxis, MyBrain.Navigator.MoveAxis.x);
            Anim.SetFloat(AnimHash.YAxis, MyBrain.Navigator.MoveAxis.y);
        }

        float _refAccel;
        void UpdateAcceleration()
        {
            Accel = Mathf.SmoothDamp(
                Accel,
                TargetAccel,
                ref _refAccel,
                0.25f,
                float.PositiveInfinity,
                Time.fixedDeltaTime);
            Anim.SetFloat(AnimHash.Accelation, Accel);
        }

        void UpdateAimWeight()
        {
            Anim.SetFloat(AnimHash.AimWeight, MyBrain.AttackController.SyncedAimWeight);
        }

        public void SetTargetAccel(float v)
        {
            TargetAccel = Mathf.Clamp(v, 0f, 4f);
        }

        public void DecideAccelByDistance()
        {
            float dist = Vector3.Distance(transform.position, MyBrain.Navigator.AI.endOfPath);
            if (!MyBrain.Sensor.IsAlert)
            {
                if (dist <= 1f)
                    SetTargetAccel(0f);
                else if (dist <= 2f)
                    SetTargetAccel(1f);
                else if (dist <= 4f)
                    SetTargetAccel(2f);
                else if (dist <= 8f)
                    SetTargetAccel(3f);
                else
                    SetTargetAccel(4f);
            }
            else
            {
                if (dist <= 1f)
                    SetTargetAccel(0f);
                else
                    SetTargetAccel(2f);
            }
        }

        public void SetAttackedDirection(Vector3 shotOrigin, LayerMask bulletOwnerLayer)
        {
            if ((bulletOwnerLayer.value & (1 << gameObject.layer)) != 0) return;
            if (MyBrain.Sensor.HasTarget) return;

            Vector3 hitDir = shotOrigin - transform.position;
            hitDir.y = 0;
            hitDir.Normalize();

            AttackedDirection = hitDir;
        }

        void HandleAttackedDirection()
        {
            if (AttackedDirection == Vector3.zero) return;

            AttackedDirection = MathUtility.IsSameDirectionXZ(transform.forward, AttackedDirection, 30f)
                ? Vector3.zero : AttackedDirection;
        }

        private bool Aimable()
        {
            var StateInfo = Anim.GetCurrentAnimatorStateInfo(0);
            return StateInfo.shortNameHash == AnimHash.Strafe;
        }

        public bool Shootable()
        {
            return AimIK.solver.IKPositionWeight >= 0.99f 
                && MyBrain.Sensor.TargetVisible
                && (MyBrain.Sensor.CurrentTargetAimPoint.position - MyBrain.GunController.AimIKTarget.position).sqrMagnitude <= 4;
        }

        private void OnDead()
        {
            AttackedDirection = Vector3.zero;
        }
    }
}