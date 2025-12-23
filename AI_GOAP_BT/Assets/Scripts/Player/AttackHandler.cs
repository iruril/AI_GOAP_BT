using Mirror;
using Player.FSM;
using UnityEngine;

namespace Player
{
    public class AttackHandler : NetworkBehaviour
    {
        PlayerController player;

        [SyncVar] private Vector3 syncedAimTarget;
        [SyncVar] private float syncedAimWeight;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
        }

        public override void OnStartServer()
        {
            player.Stat.OnDead += player.GunController.OnDead;
            player.IKManager.AimIK.solver.OnPostUpdate += player.GunController.FireCallback;
        }

        public override void OnStartClient()
        {
            player.IKManager.AimIK.solver.IKPositionWeight = 0f;
            player.IKManager.FBBIK.solver.leftHandEffector.target = player.GunController.LeftHandIKTarget;
            player.IKManager.FBBIK.solver.leftHandEffector.positionWeight = 1f;
        }

        public override void OnStopServer()
        {
            player.Stat.OnDead -= player.GunController.OnDead;
            player.IKManager.AimIK.solver.OnPostUpdate -= player.GunController.FireCallback;
        }

        private void Update()
        {
            UpdateAimValues();
            ClientUpdateIK();
        }

        private void UpdateAimValues()
        {
            syncedAimTarget = player.Input.Aim
                ? player.CamController.GetCenterWorldPoint()
                : transform.position + transform.forward * 20f + Vector3.up * 1.2f;

            syncedAimWeight = player.Input.Aim ? 1f : 0f;
        }

        Vector3 aimPosVel;
        float aimWeightVel;
        private void ClientUpdateIK()
        {
            player.GunController.AimIKTarget.position =
                Vector3.SmoothDamp(
                    player.GunController.AimIKTarget.position,
                    syncedAimTarget,
                    ref aimPosVel,
                    0.1f,
                    Mathf.Infinity,
                    Time.deltaTime
                );

            player.IKManager.AimIK.solver.IKPositionWeight =
                Mathf.SmoothDamp(
                    player.IKManager.AimIK.solver.IKPositionWeight,
                    syncedAimWeight,
                    ref aimWeightVel,
                    0.1f
                );
        }
    }
}
