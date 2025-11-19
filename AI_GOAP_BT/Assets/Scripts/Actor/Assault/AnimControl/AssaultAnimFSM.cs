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

        public float Accel { get; set; } = 0f;
        public float StateTime { get; set; }
        public bool RootRotation = false;

        float aimWeight;

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
            UpdateAccelation();
            UpdateAimWeight();
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

        void UpdateAccelation()
        {
            Anim.SetFloat(AnimHash.Accelation, Accel);
        }

        float _refAimValue;
        void UpdateAimWeight()
        {
            float _targetVaule = MySensor.TargetVisible ? 1f : 0f;
            aimWeight = Mathf.SmoothDamp(aimWeight, _targetVaule, ref _refAimValue, 0.1f);
            Anim.SetFloat(AnimHash.AimWeight, aimWeight);
        }

        private void DecideAccelByDistance()
        {
            float dist = Vector3.Distance(transform.position, Navigator.AI.endOfPath);
            if (!MySensor.HasTarget)
            {
                if (dist <= 2f)
                    Accel = 1f;
                else if (dist <= 4f)
                    Accel = 2f;
                else if (dist <= 8f)
                    Accel = 3f;
                else
                    Accel = 4f;
            }
            else Accel = 2f;
        }

        public void RecalcAccelByDistance()
        {
            DecideAccelByDistance();
        }
    }
}