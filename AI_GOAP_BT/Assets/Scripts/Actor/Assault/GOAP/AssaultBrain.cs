using UnityEngine;

namespace GOAP.Assualt
{
    public enum AssualtAction
    {
        IDLE,
        MOVE_TO_CAPTURE
    }

    public enum AssaultGoal
    {
        SURVIVE,
        CAPTURE
    }

    public class AssaultBrain : GoapBrain<AssualtAction, AssaultGoal>
    {
        public AINavigator Navigator { get; private set; }

        CapturePoint.CapturePoint currentCap; //임시. Sensor에 들어갈 것임.

        protected override void Awake()
        {
            Navigator = GetComponent<AINavigator>();
            base.Awake();
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
                    currentCap = WorldManager.Instance.RequestClosestCapture(transform, 5f, out var dest);
                    Navigator.SetDestination(dest);
                },
                OnUpdate = () =>
                {
                    if (!currentCap.NeedToCapture(transform))
                    {
                        currentCap = null;
                        Actions[AssualtAction.MOVE_TO_CAPTURE].IsFinished = true;
                    }
                },
                OnExit = () => { },

                IsUsefulForGoal = goal => goal == AssaultGoal.CAPTURE,
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
                IsSatisfied = () => true,   // 체력이 30 이상이면 생존 상태 //구현 필요
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

            DefaultGoalType = AssaultGoal.SURVIVE;
        }
    }
}
