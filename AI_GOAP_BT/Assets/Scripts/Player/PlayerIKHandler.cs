using Player.FSM;
using RootMotion.FinalIK;
using UnityEngine;

namespace Player
{
    public class PlayerIKHandler : MonoBehaviour
    {
        private PlayerController player;
        public Animator Anim { get; private set; }
        public FullBodyBipedIK FBBIK { get; private set; }
        public AimIK AimIK { get; private set; }

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
            UpdateAimWeight();
        }

        float _refAimValue;
        void UpdateAimWeight()
        {
            float _targetVaule = player.Input.Aim ? 1f : 0f;
            aimWeight = Mathf.SmoothDamp(
                aimWeight,
                _targetVaule,
                ref _refAimValue,
                player.GunController.CurrentGun.GunInfo.TimeToADS
            );
            Anim.SetFloat(AnimHash.AimWeight, aimWeight);
        }
    }
}
