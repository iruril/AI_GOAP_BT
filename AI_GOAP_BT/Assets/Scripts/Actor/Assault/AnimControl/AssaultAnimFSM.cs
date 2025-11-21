using UnityEngine;
using FSM;

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
        private AssaultAnimFSM _context => this;
        public Animator Anim { get; private set; }
        public AINavigator Navigator { get; private set; }
        public Sensor.Assualt.AssaultSensor MySensor { get; private set; }
        public Rigidbody MyRigid { get; private set; }

        public AnimState CurrentStateKey;

        public float Accel { get; private set; } = 0f;
        public float TargetAccel { get; private set; } = 0f;
        [SerializeField] private float accelSmoothSpeed = 4f;
        public float StateTime { get; set; }
        public bool RootRotation = false;

        private float shootableWeight;
        public bool Shootable { get; private set; }
        public float AimWeight { get; private set; }

        private Vector3? hitDir = null;
        private float hitRotateRemain = 0f;

        void Awake()
        {
            Anim = GetComponent<Animator>();
            Navigator = GetComponent<AINavigator>();
            Navigator.OnSetDestination = () => DecideAccelByDistance();
            MySensor = GetComponent<Sensor.Assualt.AssaultSensor>();
            MyRigid = GetComponent<Rigidbody>();

            InitializeStates();
            CurrentState = States[AnimState.Idle];
        }

        protected override void Start()
        {
            base.Start();
            MySensor.MyStat.OnDead += () =>
            {
                hitDir = null;
                hitRotateRemain = 0f;
            };
        }

        void OnAnimatorMove()
        {
            if (Time.deltaTime <= 0) return;

            Vector3 nextPosition;
            Quaternion nextRotation;
            Navigator.AI.MovementUpdate(Time.fixedDeltaTime, out nextPosition, out nextRotation);

            Vector3 rootPosition = new Vector3(Anim.rootPosition.x, nextPosition.y, Anim.rootPosition.z);

            if (!Navigator.AI.enableRotation && RootRotation)
            {
                Navigator.AI.FinalizeMovement(rootPosition, nextRotation);
                this.transform.rotation *= Anim.deltaRotation;
            }
            else
                Navigator.AI.FinalizeMovement(rootPosition, nextRotation);
        }

        protected override void Update()
        {
            base.Update();
            CurrentStateKey = CurrentState.StateKey;
            UpdateMoveAxis();
            UpdateAcceleration();
            UpdateAimWeight();
            UpdateShootableCondition();
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
            Anim.SetFloat(AnimHash.XAxis, Navigator.MoveAxis.x);
            Anim.SetFloat(AnimHash.YAxis, Navigator.MoveAxis.y);
        }

        void UpdateAcceleration()
        {
            Accel = Mathf.Lerp(Accel, TargetAccel, Time.deltaTime * accelSmoothSpeed);
            Anim.SetFloat(AnimHash.Accelation, Accel);
        }

        float _refAimValue;
        void UpdateAimWeight()
        {
            float _targetVaule = MySensor.TargetVisible ? 1f : 0f;
            AimWeight = Mathf.SmoothDamp(AimWeight, _targetVaule, ref _refAimValue, 0.1f);
            Anim.SetFloat(AnimHash.AimWeight, AimWeight);
        }

        void UpdateShootableCondition()
        {
            shootableWeight = Anim.GetFloat(AnimHash.Shootable);
            Shootable = shootableWeight >= 0.99f && AimWeight >= 0.99f;
        }

        public void SetTargetAccel(float v)
        {
            TargetAccel = Mathf.Clamp(v, 0f, 4f);
        }

        public void DecideAccelByDistance()
        {
            float dist = Vector3.Distance(transform.position, Navigator.AI.endOfPath);
            if (!MySensor.HasTarget)
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
                SetTargetAccel(2f);
        }

        public void OnHit(Vector3 dir)
        {
            hitDir = dir;
            hitRotateRemain = 0.5f;
        }

        public void LookHitDirection()
        {
            if (MySensor.HasTarget)
            {
                hitDir = null;
                return;
            }

            if (hitDir.HasValue)
            {
                if (!RootRotation)
                {
                    Navigator.AI.enableRotation = false;
                    Quaternion targetRot = Quaternion.LookRotation(hitDir.Value);

                    float maxStep = MySensor.MyStat.RotateSpeedToTarget * Time.fixedDeltaTime;
                    Quaternion newRot = Quaternion.RotateTowards(MyRigid.rotation, targetRot, maxStep);

                    MyRigid.MoveRotation(newRot);
                }

                hitRotateRemain -= Time.fixedDeltaTime;

                if (hitRotateRemain <= 0f)
                {
                    Navigator.AI.enableRotation = true;
                    hitDir = null;
                }
            }
        }
    }
}