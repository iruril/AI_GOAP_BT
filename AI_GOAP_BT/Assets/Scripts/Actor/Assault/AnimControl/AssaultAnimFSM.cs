using System.Collections.Generic;
using UnityEngine;
using MEC;
using System;
using FSM;

namespace AnimControl.Assault
{
    public enum AnimState
    {
        Idle,
        Start,
        Move,
        Stop
    }

    public class AssaultAnimFSM : StateManager<AnimState>
    {
        private AssaultAnimFSM _context => this;
        public Animator Anim { get; private set; }
        public AINavigator Navigator { get; private set; }

        public AnimState CurrentStateKey;

        public float Accel { get; private set; } = 0f;

        public float StateTime { get; set; }

        void Awake()
        {
            Anim = GetComponent<Animator>();
            Navigator = GetComponent<AINavigator>();
            Navigator.OnSetDestination = () => DecideAccelInitial();

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

            if (!Navigator.AI.enableRotation)
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
        }

        private void InitializeStates()
        {
            States.Add(AnimState.Idle, new Idle(_context, AnimState.Idle));
            States.Add(AnimState.Start, new Start(_context, AnimState.Start));
            States.Add(AnimState.Move, new Move(_context, AnimState.Move));
            States.Add(AnimState.Stop, new Stop(_context, AnimState.Stop));
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

        public void DecideAccelInitial()
        {
            float dist = Vector3.Distance(transform.position, Navigator.AI.endOfPath);
            if (dist <= 2f)
                Accel = 1f;
            else if (dist <= 4f)
                Accel = 2f;
            else if (dist <= 8f)
                Accel = 3f;
            else
                Accel = 4f;
        }
    }
}