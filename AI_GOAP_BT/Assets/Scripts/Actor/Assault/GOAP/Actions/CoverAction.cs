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
            return brain.Sensor.MyStat.CurrentHP <= brain.Sensor.MyStat.MaxHP * 0.3f;
        }

        public override bool IsUsefulForGoal(AssaultGoal goal)
        {
            return goal == AssaultGoal.SURVIVE || goal == AssaultGoal.ENGAGE_ENEMY;
        }

        public override void OnStart()
        {
            coverRecalcPending = false;
            brain.Sensor.MyStat.OnGrazeBullet += RecalcCoverPosition;
            MoveToBestCover();
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
            brain.Sensor.MyStat.OnGrazeBullet -= RecalcCoverPosition;

            if (brain.Sensor.LastSeenPosition != Vector3.negativeInfinity)
            {
                brain.EQS.LoadContext("Peek");
                brain.EQS.TickEQS();

                if (brain.EQS.BestItem != null)
                    brain.Navigator.SetDestination(brain.EQS.BestItem.GetWorldPosition());
            }
        }

        private void MoveToBestCover()
        {
            if (brain.Sensor.LastSeenPosition == Vector3.negativeInfinity) return;

            brain.EQS.LoadContext("Cover");
            brain.EQS.TickEQS();

            if (brain.EQS.BestItem != null)
                brain.Navigator.SetDestination(brain.EQS.BestItem.GetWorldPosition());
        }

        private void RecalcCoverPosition(Vector3 shotOrigin, LayerMask bulletOwnerLayer)
        {
            if ((bulletOwnerLayer.value & (1 << brain.gameObject.layer)) != 0) return;
            if (coverRecalcPending || brain.CurrentAction.Type != Type) return;

            coverRecalcPending = true;

            Vector3 aimIKOrigin = brain.GunController.AimIKTarget.position;
            try
            {
                brain.GunController.AimIKTarget.position = shotOrigin;
                MoveToBestCover();
            }
            finally
            {
                brain.GunController.AimIKTarget.position = aimIKOrigin;
            }

            recalcHandle = Timing.RunCoroutine(ResetFlagNextFrame());
        }

        private IEnumerator<float> ResetFlagNextFrame()
        {
            yield return Timing.WaitForOneFrame;
            coverRecalcPending = false;
        }
    }
}
