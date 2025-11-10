using System;
using UnityEngine;

namespace FSM
{
    public abstract class BaseState<Estate> where Estate : Enum
    {
        public BaseState(Estate key)
        {
            StateKey = key;
        }

        public Estate StateKey { get; private set; }

        public abstract void EnterState();
        public abstract void ExitState();
        public abstract void UpdateState(); //Tick -> Update()에서 호출한다.
        public abstract void PhysicsUpdateState(); //Physics Tick -> 물리연산은 무겁다. FixedUpdate()에서 호출한다.
        public abstract Estate GetNextState();
        public abstract void OnTriggerEnter(Collider other);
        public abstract void OnTriggerStay(Collider other);
        public abstract void OnTriggerExit(Collider other);
    }
}
