using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace GOAP
{
    public class GoapAction<ActionType, GoalType> where ActionType : Enum where GoalType : Enum
    {
        public ActionType Type { get; protected set; }
        public int Cost { get; protected set; } = 1;
        public virtual bool CheckPreconditions() => true;
        public virtual bool IsUsefulForGoal(GoalType goal) => true;
        public bool IsFinished { get; protected set; } = false;

        public virtual void OnStart() { }
        public virtual void OnPhysicsUpdate() { }
        public virtual void OnUpdate() { }
        public virtual void OnExit() { }

        public void Complete()
        {
            IsFinished = true;
        }
        public virtual void Reset()
        {
            IsFinished = false;
        }
    }

    public class GoapGoal<GoalType> where GoalType : Enum
    {
        public GoalType Type;
        public int Priority = 0;
        public Func<bool> IsSatisfied;
        public bool Repeatable = true;
    }

    public abstract class GoapBrain<ActionType, GoalType> : NetworkBehaviour where ActionType : Enum where GoalType : Enum
    {
        //public AISensor sensor;
        //public AIBlackboard blackboard;

        protected Dictionary<ActionType, GoapAction<ActionType, GoalType>> Actions = new();
        protected Dictionary<GoalType, GoapGoal<GoalType>> Goals = new();

        public GoapAction<ActionType, GoalType> CurrentAction;

        public GoapGoal<GoalType> CurrentGoal;

        protected ActionType DefaultActionType;
        protected GoalType DefaultGoalType; 
        
        private float nextGoalCheck = 0f;
        [SerializeField] private float GoalCheckInterval = 0.1f;

        bool actionStarted = false;

        protected virtual void Awake()
        {
            syncInterval = 0.01f;
            RegisterActions();
            RegisterGoals();
            InitGOAP();
        }

        protected virtual void Start()
        {

        }

        protected virtual void Update()
        {
            if (!isServer) return;
            Tick_Update();
        }

        protected virtual void FixedUpdate()
        {
            if (!isServer) return;
            Tick_Physics();
        }

        #region Register ACTION / GOAL Section
        protected virtual void RegisterActions()
        {
            /*
                여기에 GoapAction을 Register
            */
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

            //// Default Goal 지정
            //DefaultGoalType = GoalType.SURVIVE;
        }

        protected virtual void InitGOAP()
        {
            if (Goals.Count == 0)
            {
                Debug.LogError("GOAP: No goals registered!"); 
                enabled = false;
                return;
            }
            else
            {
                if (Goals.TryGetValue(DefaultGoalType, out var goal)) CurrentGoal = goal;
                else CurrentGoal = Goals.First().Value;
            }

            if (Actions.Count == 0)
            {
                Debug.LogError("GOAP: No actions registered!");
                enabled = false;
                return;
            }
            else
            {
                if (Actions.TryGetValue(DefaultActionType, out var action)) CurrentAction = action;
                else CurrentAction = Actions.First().Value;
            }
        }
        #endregion

        void Tick_Update()
        {
            if (Time.time >= nextGoalCheck)
            {
                SelectGoal();
                TryChangeAction();

                nextGoalCheck = Time.time + GoalCheckInterval;
            }

            if (!actionStarted)
            {
                CurrentAction.OnStart();
                actionStarted = true;
                return;
            }

            CurrentAction.OnUpdate();
        }

        void Tick_Physics()
        {
            if (!actionStarted)
            {
                CurrentAction.OnStart();
                actionStarted = true;
                return;
            }

            CurrentAction.OnPhysicsUpdate();
        }

        void SelectGoal()
        {
            GoapGoal<GoalType> best = null;
            int bestPriority = int.MinValue;

            foreach (var g in Goals.Values)
            {
                if (!g.IsSatisfied())
                {
                    if (g.Priority > bestPriority)
                    {
                        best = g;
                        bestPriority = g.Priority;
                    }
                }
            }

            if (best != null)
            {
                CurrentGoal = best;
            }
            else if (!Goals.TryGetValue(DefaultGoalType, out CurrentGoal))
            {
                CurrentGoal = Goals.Values.First();
            }
        }

        void TryChangeAction()
        {
            bool changed = false;
            var best = SelectBestAction(CurrentGoal);

            if (!ReferenceEquals(best, CurrentAction))
            {
                StopCurrentAction();
                CurrentAction = best;
                changed = true;
            }

            if (!changed && (!CheckPreconditions(CurrentAction) || CurrentAction.IsFinished))
            {
                StopCurrentAction();
            }
        }

        GoapAction<ActionType, GoalType> SelectBestAction(GoapGoal<GoalType> goal)
        {
            GoapAction<ActionType, GoalType> best = null;
            int bestScore = int.MinValue;

            foreach (var pair in Actions)
            {
                var action = pair.Value;

                if (!CheckPreconditions(action))
                    continue;

                if (!action.IsUsefulForGoal(goal.Type))
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
            return a.CheckPreconditions();
        }

        void StopCurrentAction()
        {
            CurrentAction.OnExit();
            CurrentAction.Reset();
            actionStarted = false;
        }
    }
}
