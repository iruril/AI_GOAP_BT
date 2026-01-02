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
        }

        public void Update()
        {
            if (!player.isLocalPlayer) return;
            if (GameManager.GetInstance().InputMap.IsOnStaticUI) return;

            camTarget.position = transform.position + Vector3.up;
            CamTargetRotate(); 
            ZoonHandle();
        }

        private void CamTargetRotate()
        {
            yRotation = CamTarget.transform.eulerAngles.y + player.Input.YRotationEuler;
            xRotation = xRotation + player.Input.XRotationEuler;
            xRotation = Mathf.Clamp(xRotation, -60, 60);

            CamTarget.transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
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
    }
}
