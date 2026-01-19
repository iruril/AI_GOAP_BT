using UnityEngine;

namespace Player
{
    public class TPSCamController : MonoBehaviour
    {
        private Player.FSM.PlayerController player;

        [SerializeField] private Transform camTarget;
        public Transform CamTarget { get { return camTarget; } }

        private float yRotation = 0;
        private float xRotation = 0;
        public float YRotation { get => yRotation; private set => yRotation = value; }
        public float XRotation { get => xRotation; private set => xRotation = value; }

        private RaycastHit[] centerHits = new RaycastHit[4];

        private void Awake()
        {
            player = GetComponent<Player.FSM.PlayerController>();
        }

        public void InitCam()
        {
            MainCamManager.Instance.SetCamTarget(CamTarget);

            camTarget.transform.parent = null;
            camTarget.rotation = transform.rotation;
            YRotation = camTarget.eulerAngles.y;
        }

        public void Update()
        {
            if (!player.isLocalPlayer) return;
            if (GameManager.GetInstance().InputMap.IsOnStaticUI) return;

            camTarget.position = transform.position + Vector3.up;
            CamTargetRotate(); 
            ZoonHandle(); 
            LeanHandle();
        }

        private void CamTargetRotate()
        {
            YRotation += player.Input.YRotationEuler;
            XRotation += player.Input.XRotationEuler;
            XRotation = Mathf.Clamp(XRotation, -60, 60);

            CamTarget.transform.rotation = Quaternion.Euler(XRotation, YRotation, 0);
        }

        public Vector3 GetCenterWorldPoint(float maxDistance = 300f)
        {
            Camera cam = MainCamManager.Instance.MainCam;
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

        private void ZoonHandle()
        {
            if (player.IKManager.IsOnAim)
                MainCamManager.Instance.ActivateAimModeCam();
            else
                MainCamManager.Instance.ActivateDefaultModeCam();
        }

        private void LeanHandle()
        {
            if(player.Input.LeanLeft)
                MainCamManager.Instance.Lean(true);
            else
                MainCamManager.Instance.Lean(false);
        }
    }
}
