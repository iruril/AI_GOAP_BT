using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace GOAP
{
    public enum ActionType
    {
        NONE,
        MOVE_TO_CAPTURE,
    }

    public enum GoalType
    {
        NONE,
        SURVIVE,
        ENGAGE,
        CAPTURE
    } 

    public class GoapAction
    {
        public ActionType Type = ActionType.NONE;
        public int Cost = 1;

        public List<Func<bool>> Preconditions = new();
        public List<Action> Effects = new();
    }

    public class GoapGoal
    {
        public GoalType Type = GoalType.NONE;
        public int Priority = 0;
        public Func<bool> IsSatisfied;
    }

    public class GoapBrain : MonoBehaviour
    {
        //public AISensor sensor;
        //public AIBlackboard blackboard;

        public Dictionary<ActionType, GoapAction> Actions = new();
        public Dictionary<GoalType, GoapGoal> Goals = new();

        public ActionType CurrentAction { get; private set; } = ActionType.NONE;
        public GoalType CurrentGoal { get; private set; } = GoalType.NONE;

        protected virtual void Awake()
        {

            RegisterActions();
            RegisterGoals();
        }

        protected virtual void FixedUpdate()
        {
            Tick();
        }

        #region Register ACTION / GOAL Section
        protected virtual void RegisterActions()
        {
            //등록 예시
            //Actions.Add(ActionType.MOVE_TO_CAPTURE, new GoapAction
            //{
            //    Type = ActionType.MOVE_TO_CAPTURE,
            //    Cost = 10,
            //    Preconditions =
            //    {
            //        () => !sensor.HasTarget,
            //        () => !blackboard.InRetreat
            //    },
            //    Effects =
            //    {
            //        () => blackboard.AtCapturePoint = true
            //    }
            //});
        }

        protected virtual void RegisterGoals()
        {
            //등록 예시
            //Goals.Add(GoalType.SURVIVE, new GoapGoal
            //{
            //    Type = GoalType.SURVIVE,
            //    Priority = 100,
            //    IsSatisfied = () => { return true; }
            //});

            //Goals.Add(GoalType.ENGAGE, new GoapGoal
            //{
            //    Type = GoalType.ENGAGE,
            //    Priority = 60,
            //    IsSatisfied = () => { return true; }
            //});

            //Goals.Add(GoalType.CAPTURE, new GoapGoal
            //{
            //    Type = GoalType.CAPTURE,
            //    Priority = 10,
            //    IsSatisfied = () => { return true; }
            //});
        }
        #endregion

        private void Tick()
        {
            var unsatisfied = Goals
                .Where(g => !g.Value.IsSatisfied())
                .OrderByDescending(g => g.Value.Priority);

            if (!unsatisfied.Any())
            {
                CurrentGoal = GoalType.NONE;
                CurrentAction = ActionType.NONE;
                return;
            }

            var goal = unsatisfied.First();
            CurrentGoal = goal.Key;

            var current = CurrentAction != ActionType.NONE ? Actions[CurrentAction] : null;

            // 현재 액션이 여전히 유효하면 유지
            if (current != null && CheckPreconditions(current))
                return;

            // 아니면 새 액션 선택
            CurrentAction = SelectBestAction(goal.Value);
        }

        private ActionType SelectBestAction(GoapGoal goal)
        {
            ActionType best = ActionType.NONE;
            int bestScore = int.MinValue;

            foreach (var action in Actions)
            {
                if (!CheckPreconditions(action.Value)) continue;

                int score = -action.Value.Cost;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = action.Key;
                }
            }

            return best;
        }

        private bool CheckPreconditions(GoapAction a)
        {
            foreach (var cond in a.Preconditions)
                if (!cond()) return false;

            return true;
        }

        /// <summary>
        /// 현재 액션이 완수되었음을 외부에서 알리는 용도.
        /// 완수되었다면 CurrentAction의 Effect를 적용한다.
        /// </summary>
        public void CompleteCurrentAction()
        {
            if (CurrentAction == ActionType.NONE) return;

            foreach (var eff in Actions[CurrentAction].Effects)
                eff?.Invoke();

            CurrentAction = ActionType.NONE;
        }
    }
}
