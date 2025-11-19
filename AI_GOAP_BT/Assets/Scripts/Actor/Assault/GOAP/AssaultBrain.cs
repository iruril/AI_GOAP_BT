using UnityEngine;

namespace GOAP.Assualt
{
    public enum AssualtAction
    {
        IDLE,
        MOVE_TO_CAPTURE,
        COMBAT
    }

    public enum AssaultGoal
    {
        SURVIVE,
        CAPTURE,
        ENGAGE_ENEMY
    }

    public class AssaultBrain : GoapBrain<AssualtAction, AssaultGoal>
    {
        public AINavigator Navigator { get; private set; }

        public Sensor.Assualt.AssaultSensor Sensor { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Navigator = GetComponent<AINavigator>();
            Sensor = GetComponent<Sensor.Assualt.AssaultSensor>();
        }

        protected override void Start()
        {
            Sensor.MyStat.OnDead += () =>
            {
                InitGOAP();
            };
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
        }

        protected override void RegisterActions()
        {
            Actions.Add(AssualtAction.IDLE, new GoapAction<AssualtAction, AssaultGoal>
            {
                Type = AssualtAction.IDLE,
                Cost = 50,

                Preconditions =
                {
                    () => true // 기본 Idle은 항상 실행 가능
                },

                OnStart = () => { },
                OnUpdate = () => { },
                OnExit = () => { },

                IsUsefulForGoal = goal => true, // 어떤 Goal에도 기본 Idle은 유효
                IsFinished = false
            });

            Actions.Add(AssualtAction.MOVE_TO_CAPTURE, new GoapAction<AssualtAction, AssaultGoal>
            {
                Type = AssualtAction.MOVE_TO_CAPTURE,
                Cost = 10,

                Preconditions =
                {
                    () => WorldManager.Instance.IsThereUncapturedPoint(transform)
                },

                OnStart = () =>
                {
                    Sensor.GetClosestCapture(out var destination);
                    Navigator.SetDestination(destination);
                },
                OnUpdate = () =>
                {
                    if (Sensor.IsCurrentCapCapturerd())
                    {
                        CompleteCurrentAction();
                    }
                },
                OnExit = () =>
                {
                    Sensor.ResetCapture();
                },

                IsUsefulForGoal = goal => goal == AssaultGoal.CAPTURE,
                IsFinished = false
            });

            Actions.Add(AssualtAction.COMBAT, new GoapAction<AssualtAction, AssaultGoal>
            {
                Type = AssualtAction.COMBAT,
                Cost = 10,

                Preconditions =
                {
                    () => Sensor.HasTarget
                },

                OnStart = () => { },
                OnUpdate = () =>
                {
                    if (!Sensor.HasTarget)
                    {
                        CompleteCurrentAction();
                    }
                },
                OnExit = () => { },

                IsUsefulForGoal = goal => goal == AssaultGoal.CAPTURE || goal == AssaultGoal.ENGAGE_ENEMY,
                IsFinished = false
            });

            DefaultActionType = AssualtAction.IDLE;
        }

        protected override void RegisterGoals()
        {
            Goals.Add(AssaultGoal.SURVIVE, new GoapGoal<AssaultGoal>
            {
                Type = AssaultGoal.SURVIVE,
                Priority = 100,
                IsSatisfied = () => 
                {
                    return true; //Sensor.MyStat.CurrentHP >= 30f;
                },
                Repeatable = true
            });

            Goals.Add(AssaultGoal.CAPTURE, new GoapGoal<AssaultGoal>
            {
                Type = AssaultGoal.CAPTURE,
                Priority = 20,
                IsSatisfied = () =>
                {
                    return !WorldManager.Instance.IsThereUncapturedPoint(transform);
                },
                Repeatable = true
            });

            Goals.Add(AssaultGoal.ENGAGE_ENEMY, new GoapGoal<AssaultGoal>
            {
                Type = AssaultGoal.ENGAGE_ENEMY,
                Priority = 50,
                IsSatisfied = () =>
                {
                    return Sensor.CurrentTarget == null;
                },
                Repeatable = true
            });

            DefaultGoalType = AssaultGoal.SURVIVE;
        }
    }
}
