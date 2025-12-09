using MEC;
using System.Collections.Generic;
using UnityEngine;

namespace GOAP.Assualt
{
    public class ReloadAction : GoapAction<AssualtAction, AssaultGoal>
    {
        AssaultBrain brain;
        private bool coverRecalcPending = false;
        private bool reloadStarted = false;

        private float timer = 0;
        private const float RELOAD_EXECUTE_TIME = 2.0f;

        private CoroutineHandle recalcHandle;

        public ReloadAction(AssaultBrain brain, AssualtAction action, int cost)
        {
            this.brain = brain;
            Type = action;
            Cost = cost;
        }

        public override bool CheckPreconditions()
        {
            return brain.GunController.CurrentRounds <= brain.GunController.CurrentGun.GunInfo.MagazineCapacity * 0.2f;
        }

        public override bool IsUsefulForGoal(AssaultGoal goal)
        {
            return true; // 탄약이 부족하면 언제든 재장전
        }

        public override void OnStart()
        {
            timer = 0f;
            reloadStarted = false;

            brain.Sensor.MyStat.OnUnderAttack += RecalcCoverPosition;
            if (brain.Sensor.LastSeenPosition != Vector3.negativeInfinity)
            {
                brain.EQS.LoadContext("Cover");
                brain.EQS.TickEQS();
                brain.Navigator.SetDestination(brain.EQS.BestItem.GetWorldPosition());
            }
        }

        public override void OnPhysicsUpdate()
        {
            if (!reloadStarted)
            {
                timer += Time.fixedDeltaTime;

                if (timer >= RELOAD_EXECUTE_TIME || brain.GunController.CurrentRounds == 0 || !brain.Sensor.HasTarget)
                {
                    reloadStarted = true;
                    brain.GunController.Reload(brain.MotionController.Anim, brain.MotionController.FBBIK.solver.leftHandEffector);
                }
            }

            if (brain.GunController.CurrentRounds >= brain.GunController.CurrentGun.GunInfo.MagazineCapacity)
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

            Vector3 aimIKOrigin = brain.GunController.AimIKTarget.position;
            brain.GunController.AimIKTarget.position = shotOrigin;
            brain.EQS.LoadContext("Cover");
            brain.EQS.TickEQS();
            brain.Navigator.SetDestination(brain.EQS.BestItem.GetWorldPosition());
            brain.GunController.AimIKTarget.position = aimIKOrigin;

            recalcHandle = Timing.RunCoroutine(ResetFlagNextFrame());
        }

        private IEnumerator<float> ResetFlagNextFrame()
        {
            yield return Timing.WaitForOneFrame;
            coverRecalcPending = false;
        }
    }
}