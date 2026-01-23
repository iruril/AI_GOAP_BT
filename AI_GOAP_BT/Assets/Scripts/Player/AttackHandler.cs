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

        private float rateOfFireTime = 0;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
        }

        private void OnDisable()
        {
            syncedAimWeight = 0f;
            player.IKManager.AimIK.solver.IKPositionWeight = 0f;
            player.IKManager.LookIK.solver.IKPositionWeight = 0f;
        }

        public override void OnStartLocalPlayer()
        {
            player.IKManager.AimIK.solver.OnPostUpdate += player.GunController.ClientFireCallback;
        }

        public override void OnStartServer()
        {
            player.MyStat.OnDead += player.GunController.OnDead;
        }

        public override void OnStartClient()
        {
            player.IKManager.AimIK.solver.IKPositionWeight = 0f;
            player.IKManager.LookIK.solver.IKPositionWeight = 0f;
            player.IKManager.FBBIK.solver.leftHandEffector.target = player.GunController.LeftHandIKTarget;
            player.IKManager.FBBIK.solver.leftHandEffector.positionWeight = 1f;
        }

        public override void OnStopLocalPlayer()
        {
            player.IKManager.AimIK.solver.OnPostUpdate -= player.GunController.ClientFireCallback;
        }

        public override void OnStopServer()
        {
            player.MyStat.OnDead -= player.GunController.OnDead;
        }

        private void Update()
        {
            UpdateIK();

            if (!isLocalPlayer) return;

            UpdateAimValues();
            UpdateRateOfFire();
            TryShoot(); 
            TryReload();
        }

        private void UpdateAimValues()
        {
            syncedAimTarget = player.IKManager.IsOnAim
                ? player.CamController.GetCenterWorldPoint()
                : transform.position + transform.forward * 20f + Vector3.up * 1.2f;

            syncedAimWeight = player.IKManager.IsOnAim && !player.IKManager.IsGunInWall && !player.GunController.OnReload
                ? 1f : 0f;
        }

        float aimWeightVel;
        private void UpdateIK()
        {
            player.GunController.AimIKTarget.position = syncedAimTarget;

            player.IKManager.AimIK.solver.IKPositionWeight =
                Mathf.SmoothDamp(
                    player.IKManager.AimIK.solver.IKPositionWeight,
                    syncedAimWeight,
                    ref aimWeightVel,
                    0.1f
                );
            player.IKManager.LookIK.solver.IKPositionWeight = player.IKManager.AimIK.solver.IKPositionWeight;
        }

        private void UpdateRateOfFire()
        {
            if (rateOfFireTime <= 0f)
                return;

            rateOfFireTime -= Time.deltaTime;

            if (rateOfFireTime < 0f)
                rateOfFireTime = 0f;
        }

        private void TryShoot()
        {
            if (!player.Input.Trigger || player.IKManager.AimIK.solver.IKPositionWeight < 0.99f)
                return;

            if (rateOfFireTime > 0f)
                return;

            rateOfFireTime = player.GunController.CurrentGun.GunInfo.ShotInterval;
            Shoot();
        }

        private void Shoot()
        {
            if (player.GunController.CurrentRounds <= 0) return;
            player.GunController.Fire();
        }

        private void TryReload()
        {
            if (!player.Input.Reload || GameManager.GetInstance().InputMap.IsOnStaticUI) return;

            if (player.GunController.CurrentRounds >= player.GunController.CurrentGun.GunInfo.MagazineCapacity + 1)
                return;

            player.GunController.Reload();
        }
    }
}
