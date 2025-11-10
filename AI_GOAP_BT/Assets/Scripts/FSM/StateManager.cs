using System;
using System.Collections.Generic;
using UnityEngine;

namespace FSM
{
    public abstract class StateManager<EState> : MonoBehaviour where EState : Enum
    {
        protected Dictionary<EState, BaseState<EState>> States = new();

        protected BaseState<EState> CurrentState;

        protected bool IsTransitioningState = false; //State 변환 중에 중복해서 State 변환이 호출 될 필요는 없다.

        private EState _nextStateKey; //다음 State의 키 값
        private EState _prevStateKey; //이전 State의 키 값

        private void Start()
        {
            CurrentState.EnterState();
        }

        protected virtual void Update()
        {
            _nextStateKey = CurrentState.GetNextState();

            if (!IsTransitioningState && _nextStateKey.Equals(CurrentState.StateKey))
            {
                CurrentState.UpdateState();
            }
            else if(!IsTransitioningState)
            {
                TransitionToState(_nextStateKey);
            }
        }

        protected virtual void FixedUpdate()
        {
            if (!IsTransitioningState && _nextStateKey.Equals(CurrentState.StateKey))
            {
                CurrentState.PhysicsUpdateState();
            }
        }

        /// <summary>
        /// State Trasition을 위한 메소드.
        /// 메소드 도입부에 IsTranstioningState를 체크하여 중복 Transition을 막는다.
        /// CurrentState의 종료함수 ExitState()를 호출 한 후. 다음 State를 CurrentState로 대입한다.
        /// 이후 CurrentState의 EnterState()를 실행한다.
        /// </summary>
        /// <param name="stateKey"> 다음 State의 KeyValue </param>
        public void TransitionToState(EState stateKey)
        {
            IsTransitioningState = true;
            _prevStateKey = CurrentState.StateKey;
            CurrentState.ExitState();
            CurrentState = States[stateKey];
            CurrentState.EnterState();
            IsTransitioningState = false;
        }

        public EState GetPrevState()
        {
            if (_prevStateKey != null)
            {
                return _prevStateKey;
            }
            else
            {
                return CurrentState.StateKey;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            CurrentState.OnTriggerEnter(other);
        }

        private void OnTriggerStay(Collider other)
        {
            CurrentState.OnTriggerStay(other);
        }

        private void OnTriggerExit(Collider other)
        {
            CurrentState.OnTriggerExit(other);
        }
    }
}
