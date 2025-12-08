using MEC;
using System.Collections.Generic;
using UnityEngine;

namespace GOAP.Assualt
{
    public class CoverAction : GoapAction<AssualtAction, AssaultGoal>
    {
        AssaultBrain brain;
        private bool coverRecalcPending = false;

        private CoroutineHandle recalcHandle;

        public CoverAction(AssaultBrain brain, AssualtAction action, int cost)
        {
            this.brain = brain;
            Type = action;
            Cost = cost;
        }

        public override bool CheckPreconditions()
        {
            return brain.Sensor.MyStat.CurrentHP <= brain.Sensor.MyStat.MaxHP * 0.25f;
        }

        public override bool IsUsefulForGoal(AssaultGoal goal)
        {
            return true; // 체력이 낮으면 언제든 엄폐
        }

        public override void OnStart()
        {
            brain.Sensor.MyStat.OnUnderAttack += RecalcCoverPosition;
            if (brain.Sensor.LastSeenPosition != Vector3.negativeInfinity)
            {
                brain.EQS.LoadContext("Cover");
                brain.EQS.TickEQS();
                brain.Navigator.SetDestination(brain.EQS.BestItem.GetWorldPosition());
            }
        }

        public override void OnUpdate()
        {
            brain.AttackController.TryAttack();
        }

        public override void OnPhysicsUpdate()
        {
            if (brain.Sensor.MyStat.CurrentHP >= brain.Sensor.MyStat.MaxHP * 0.75f)
                Complete();
        }

        public override void OnExit()
        {
            Timing.KillCoroutines(recalcHandle);
            brain.Sensor.MyStat.OnUnderAttack -= RecalcCoverPosition;
            if (brain.Sensor.LastSeenPosition != Vector3.negativeInfinity)
            {
                brain.EQS.LoadContext("Peek");
                brain.EQS.TickEQS();
                brain.Navigator.SetDestination(brain.EQS.BestItem.GetWorldPosition());
            }
        }

        private void RecalcCoverPosition(Vector3 shotOrigin)
        {
            if (coverRecalcPending || brain.CurrentAction.Type != Type) return;
            coverRecalcPending = true;

            brain.GunController.AimIKTarget.position = shotOrigin;
            brain.EQS.LoadContext("Cover");
            brain.EQS.TickEQS();
            brain.Navigator.SetDestination(brain.EQS.BestItem.GetWorldPosition());

            recalcHandle = Timing.RunCoroutine(ResetFlagNextFrame());
        }

        private IEnumerator<float> ResetFlagNextFrame()
        {
            yield return Timing.WaitForOneFrame;
            coverRecalcPending = false;
        }
    }
}
