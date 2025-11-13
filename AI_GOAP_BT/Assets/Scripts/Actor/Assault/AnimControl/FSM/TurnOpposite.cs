using MEC;
using System.Collections.Generic;
using UnityEngine;

namespace AnimControl.Assault
{
    public class TurnOpposite : BaseAssaultAnimState
    {
        private float turnTime;

        public TurnOpposite(AssaultAnimFSM ctx, AnimState key) : base(ctx, key)
        {
            this.ctx = ctx;
        }

        public override void EnterState()
        {
            base.EnterState();
            ctx.Navigator.AI.enableRotation = false;
            ctx.RootRotation = true;

            Vector3 tgt = ctx.transform.position - ctx.Navigator.AI.steeringTarget;
            tgt.y = 0f;
            tgt.Normalize();

            Quaternion startRot = ctx.transform.rotation;
            Quaternion endRot = Quaternion.LookRotation(tgt);
            Timing.RunCoroutine(SmoothRotate(startRot, endRot, 0.1f));

            int snapSpeed = Mathf.Clamp(Mathf.RoundToInt(ctx.Accel), 1, 4);
            ctx.Anim.SetFloat(AnimHash.TransitionAccel, snapSpeed);
            ctx.Anim.CrossFade(AnimHash.Opposite_R, 0.1f);

            switch (snapSpeed)
            {
                case 3:
                    turnTime = 1.9f;
                    break;
                case 4:
                    turnTime = 1.1f;
                    break;
                default:
                    turnTime = 1.3f;
                    break;
            }
        }

        public override void ExitState()
        {

        }

        public override void UpdateState()
        {
            base.UpdateState();
        }

        public override void PhysicsUpdateState()
        {

        }

        public override AnimState GetNextState()
        {
            if (ctx.StateTime >= turnTime) return AnimState.Move;
            return StateKey;
        }

        public override void OnTriggerEnter(Collider other) { }

        public override void OnTriggerStay(Collider other) { }

        public override void OnTriggerExit(Collider other) { }

        IEnumerator<float> SmoothRotate(Quaternion startRot, Quaternion endRot, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                ctx.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
                yield return Timing.WaitForOneFrame;
            }

            ctx.transform.rotation = endRot;
        }
    }
}
