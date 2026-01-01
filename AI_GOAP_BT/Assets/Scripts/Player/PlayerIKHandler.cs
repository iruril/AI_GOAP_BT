using Mirror;
using Player.FSM;
using RootMotion.FinalIK;
using UnityEngine;

namespace Player
{
    public class PlayerIKHandler : NetworkBehaviour
    {
        private PlayerController player;
        public Animator Anim { get; private set; }
        public FullBodyBipedIK FBBIK { get; private set; }
        public AimIK AimIK { get; private set; }

        [SyncVar] public bool IsOnAim;
        private float aimWeight;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            Anim = GetComponent<Animator>();
            FBBIK = GetComponent<FullBodyBipedIK>();
            AimIK = GetComponent<AimIK>();
        }

        private void Update()
        {
            SyncAim();
            UpdateAimWeight();
        }

        float _refAimValue;
        void UpdateAimWeight()
        {
            float _targetVaule = IsOnAim ? 1f : 0f;
            aimWeight = Mathf.SmoothDamp(
                aimWeight,
                _targetVaule,
                ref _refAimValue,
                player.GunController.CurrentGun.GunInfo.TimeToADS
            );
            Anim.SetFloat(AnimHash.AimWeight, aimWeight);
        }

        void SyncAim()
        {
            if (!isLocalPlayer) return;

            IsOnAim = player.Input.Aim 
                && player.IsGrounded 
                && player.State != PlayerState.Land
                && !player.MyStat.IsDead;
        }
    }
}
