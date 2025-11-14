using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace GOAP
{
    public class GoapAction<ActionType, GoalType> where ActionType : Enum where GoalType : Enum
    {
        public ActionType Type;
        public int Cost = 1;

        public List<Func<bool>> Preconditions = new();
        public List<Action> Effects = new();

        public Action OnStart;
        public Action OnUpdate;
        public Action OnExit;

        public bool IsFinished;

        public Func<GoalType, bool> IsUsefulForGoal;
    }

    public class GoapGoal<GoalType> where GoalType : Enum
    {
        public GoalType Type;
        public int Priority = 0;
        public Func<bool> IsSatisfied;
        public bool Repeatable = true;
    }

    public class GoapBrain<ActionType, GoalType> : MonoBehaviour where ActionType : Enum where GoalType : Enum
    {
        //public AISensor sensor;
        //public AIBlackboard blackboard;

        public Dictionary<ActionType, GoapAction<ActionType, GoalType>> Actions = new();
        public Dictionary<GoalType, GoapGoal<GoalType>> Goals = new();

        public GoapAction<ActionType, GoalType> CurrentAction { get; private set; }
        public GoapGoal<GoalType> CurrentGoal { get; private set; }

        protected ActionType DefaultActionType;
        protected GoalType DefaultGoalType;

        bool actionStarted = false;

        protected virtual void Awake()
        {
            RegisterActions();
            RegisterGoals();

            if (Goals.TryGetValue(DefaultGoalType, out var goal)) CurrentGoal = goal;
            else CurrentGoal = Goals.First().Value;

            if (Actions.TryGetValue(DefaultActionType, out var action)) CurrentAction = action;
            else CurrentAction = Actions.First().Value;
        }

        protected virtual void FixedUpdate()
        {
            Tick();
        }

        #region Register ACTION / GOAL Section
        protected virtual void RegisterActions()
        {
            //// IDLE
            //Actions.Add(ActionType.IDLE, new GoapAction<ActionType, GoalType>
            //{
            //    Type = ActionType.IDLE,
            //    Cost = 50, // 비용이 크면 다른 액션이 먼저 선택됨

            //    Preconditions =
            //    {
            //        () => true // 항상 실행 가능
            //    },

            //    OnStart = () => Debug.Log("Idle Start"),
            //    OnUpdate = () => { /* 가만히 있거나 혹은 Idle Loop */ },
            //    OnExit = () => Debug.Log("Idle Exit"),

            //    IsUsefulForGoal = goal => true, // 어떤 Goal에도 기본 Idle은 유효

            //    IsFinished = false // Idle은 계속 유지됨
            //});

            //Actions.Add(ActionType.PATROL, new GoapAction<ActionType, GoalType>
            //{
            //    Type = ActionType.PATROL,
            //    Cost = 10,

            //    Preconditions =
            //    {
            //        () => currentEnemy == null // 적이 없을 때만 정찰하기
            //    },

            //    OnStart = () =>
            //    {
            //        Debug.Log("Patrol Start");
            //        StartPatrol();
            //    },

            //    OnUpdate = () => UpdatePatrol(),

            //    OnExit = () => StopPatrol(),

            //    IsUsefulForGoal = goal =>
            //        goal == GoalType.FIND_ENEMY, // FIND_ENEMY Goal을 만족시키는 행동

            //    IsFinished = false // Patrol은 외부에서 완료시키는 형태
            //});

            //Actions.Add(ActionType.MOVE_TO_ENEMY, new GoapAction<ActionType, GoalType>
            //{
            //    Type = ActionType.MOVE_TO_ENEMY,
            //    Cost = 5,

            //    Preconditions =
            //    {
            //        () => currentEnemy != null,
            //        () => !enemyInAttackRange
            //    },

            //    OnStart = () =>
            //    {
            //        Debug.Log("MoveToEnemy Start");
            //        navAgent.SetDestination(currentEnemy.position);
            //    },

            //    OnUpdate = () =>
            //    {
            //        navAgent.SetDestination(currentEnemy.position);
            //        if (enemyInAttackRange)
            //            Actions[ActionType.MOVE_TO_ENEMY].IsFinished = true;
            //    },

            //    OnExit = () => navAgent.ResetPath(),

            //    IsUsefulForGoal = goal =>
            //        goal == GoalType.ATTACK || goal == GoalType.FIND_ENEMY,

            //    IsFinished = false
            //});

            //Actions.Add(ActionType.ATTACK, new GoapAction<ActionType, GoalType>
            //{
            //    Type = ActionType.ATTACK,
            //    Cost = 1,

            //    Preconditions =
            //    {
            //        () => currentEnemy != null,
            //        () => enemyInAttackRange
            //    },

            //    OnStart = () =>
            //    {
            //        Debug.Log("Attack Start");
            //        animator.Play("Attack");
            //    },

            //    OnUpdate = () =>
            //    {
            //        // 공격 애니메이션 끝나면 행동 종료 플래그를 세터
            //        if (attackAnimationFinished)
            //            Actions[ActionType.ATTACK].IsFinished = true;
            //    },

            //    OnExit = () =>
            //    {
            //        Debug.Log("Attack Exit");
            //    },

            //    IsUsefulForGoal = goal =>
            //        goal == GoalType.ATTACK,

            //    IsFinished = false
            //});

            //DefaultActionType = ActionType.IDLE;
        }

        protected virtual void RegisterGoals()
        {
            //Goals.Add(GoalType.SURVIVE, new GoapGoal<GoalType>
            //{
            //    Type = GoalType.SURVIVE,
            //    Priority = 0,
            //    IsSatisfied = () => health > 30f,   // 체력이 30 이상이면 생존 상태
            //    Repeatable = true
            //});

            //Goals.Add(GoalType.ATTACK, new GoapGoal<GoalType>
            //{
            //    Type = GoalType.ATTACK,
            //    Priority = 1,
            //    IsSatisfied = () => enemyInAttackRange, // 사정거리 안에 적이 있다면 목표 달성됨
            //    Repeatable = true
            //});

            //Goals.Add(GoalType.FIND_ENEMY, new GoapGoal<GoalType>
            //{
            //    Type = GoalType.FIND_ENEMY,
            //    Priority = 2,
            //    IsSatisfied = () => currentEnemy != null,
            //    Repeatable = true
            //});

            //// Default Goal 지정
            //DefaultGoalType = GoalType.SURVIVE;
        }
        #endregion

        void Tick()
        {
            SelectGoal();
            RunAction();
        }

        void SelectGoal()
        {
            var unsatisfied = Goals
                .Where(g => g.Value.IsSatisfied() == false)
                .OrderBy(g => g.Value.Priority);

            CurrentGoal = unsatisfied.Any()
                ? unsatisfied.First().Value
                : Goals[DefaultGoalType];
        }

        void RunAction()
        {
            if (!CheckPreconditions(CurrentAction))
            {
                StopCurrentAction();
            }
            else if (CurrentAction.IsFinished)
            {
                FinishCurrentAction();
            }

            if (!actionStarted)
            {
                CurrentAction.OnStart?.Invoke();
                actionStarted = true;
            }

            CurrentAction.OnUpdate?.Invoke();
        }

        protected virtual GoapAction<ActionType, GoalType> SelectBestAction(GoapGoal<GoalType> goal)
        {
            GoapAction<ActionType, GoalType> best = null;
            int bestScore = int.MinValue;

            foreach (var pair in Actions)
            {
                var action = pair.Value;

                if (!CheckPreconditions(action))
                    continue;

                if (action.IsUsefulForGoal != null && !action.IsUsefulForGoal(goal.Type))
                    continue;

                int score = -action.Cost;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = action;
                }
            }

            return best ?? Actions[DefaultActionType];
        }

        bool CheckPreconditions(GoapAction<ActionType, GoalType> a)
        {
            foreach (var cond in a.Preconditions)
                if (!cond()) return false;

            return true;
        }

        void StopCurrentAction()
        {
            CurrentAction.IsFinished = false;
            CurrentAction.OnExit?.Invoke();
            actionStarted = false;
            CurrentAction = SelectBestAction(CurrentGoal);
        }

        void FinishCurrentAction()
        {
            foreach (var eff in CurrentAction.Effects)
                eff?.Invoke();

            StopCurrentAction();
        }

        /// <summary>
        /// 현재 액션이 완수되었음을 외부에서 알리는 용도.
        /// 완수되었다면 CurrentAction의 Effect를 적용한다.
        /// </summary>
        public void CompleteCurrentAction()
        {
            CurrentAction.IsFinished = true;
        }
    }
}
