using UnityEngine;

namespace Player
{
    public class TPSCamController : MonoBehaviour
    {
        private Player.FSM.PlayerController player;
        private RecoilController recoil;

        [Header("Camera Target")]
        [SerializeField] private Transform camTarget;
        public Transform CamTarget { get { return camTarget; } }

        [Header("Recoil Target")]
        [SerializeField] private Transform recoilTarget;
        public Transform RecoilTarget { get { return recoilTarget; } }

        private float yRotation = 0;
        private float xRotation = 0;
        public float YRotation { get => yRotation; private set => yRotation = value; }
        public float XRotation { get => xRotation; private set => xRotation = value; }
        private float initYRotation;

        private RaycastHit[] centerHits = new RaycastHit[4];

        private void Awake()
        {
            player = GetComponent<Player.FSM.PlayerController>();
            recoil = recoilTarget.GetComponent<RecoilController>();
        }

        public void InitCam()
        {
            CameraManager.Instance.SetCamTarget(RecoilTarget);

            camTarget.parent = null;
            camTarget.rotation = transform.rotation;
            YRotation = camTarget.eulerAngles.y;
            initYRotation = YRotation;

            player.MyStat.OnRevive += OnRevive;
            player.GunController.OnFired += recoil.ApplyRecoil;
            player.GunController.OnGunRecoilChanged += recoil.SetRecoilValue;

            recoil.SetRecoilValue(
                player.GunController.CurrentGun.GunInfo.RecoilPitch,
                player.GunController.CurrentGun.GunInfo.RecoilYawLeft,
                player.GunController.CurrentGun.GunInfo.RecoilYawRight,
                player.GunController.CurrentGun.GunInfo.RecoilRoll
            );
        }

        public void Update()
        {
            if (!player.isLocalPlayer) return;

            HandleCamPosition();
            HandleZoom();

            if (GameManager.GetInstance().InputMap.IsOnStaticUI) return;

            HandleRotation();
            HandleLean();
        }

        private void HandleCamPosition()
        {
            bool isOnCrouch = player.State == FSM.PlayerState.Crouch || player.State == FSM.PlayerState.CrouchIdle;
            
            float currentWeight = isOnCrouch ? player.Anim.GetFloat(AnimHash.CrouchWeight) : 0;
            float height = Mathf.Lerp(1.0f, 0.5f, currentWeight);

            Vector3 pos = transform.position;
            pos.y += height;

            CamTarget.position = pos;
        }

        private void HandleRotation()
        {
            float inputYaw = player.Input.YRotationEuler;
            float inputPitch = player.Input.XRotationEuler;

            inputPitch = recoil.ConsumePitch(inputPitch);
            inputYaw = recoil.ConsumeYaw(inputYaw);

            YRotation += inputYaw;
            XRotation += inputPitch;
            XRotation = Mathf.Clamp(XRotation, -60, 60);

            camTarget.rotation = Quaternion.Euler(XRotation, YRotation, 0);
        }

        public Vector3 GetCenterWorldPoint(float maxDistance = 300f)
        {
            Camera cam = CameraManager.Instance.MainCam;
            if (cam == null)
                return Vector3.zero;

            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            int count = Physics.RaycastNonAlloc(ray, centerHits, maxDistance, WorldManager.Instance.GetShootableLayers());

            float closestDist = float.MaxValue;
            Vector3 result = ray.origin + ray.direction * maxDistance; // 기본값: 안 맞으면 maxDistance 지점
            Transform self = player.transform;

            for (int i = 0; i < count; i++)
            {
                var hit = centerHits[i];

                if (hit.collider == null)
                    continue;

                Transform t = hit.transform;

                // 자기 자식 제외
                if (t.IsChildOf(self))
                    continue;

                // 가장 가까운 히트 선택
                if (hit.distance < closestDist)
                {
                    closestDist = hit.distance;
                    result = hit.point;
                }
            }

            return result;
        }

        private void HandleZoom()
        {
            if (player.IKManager.IsOnAim)
                CameraManager.Instance.ActivateAimModeCam();
            else
                CameraManager.Instance.ActivateDefaultModeCam();
        }

        private void HandleLean()
        {
            if (player.Input.LeanLeft)
                CameraManager.Instance.Lean(true);
            else
                CameraManager.Instance.Lean(false);
        }

        private void OnRevive()
        {
            xRotation = 0;
            yRotation = initYRotation;
            recoil.ResetRecoil();
        }
    }
}
