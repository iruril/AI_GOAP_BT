using UnityEngine;

namespace Player.FSM
{
    public class GroundChecker : MonoBehaviour
    {
        private PlayerController player;

        [Header("Boxcast로 감지할 최대 거리")]
        [SerializeField] private float detectionMaxDist;
        [Header("Boxcast 시 박스 사이즈")]
        [SerializeField] private Vector3 boxHalfExtent = new Vector3(0.5f, 0.3f, 0.5f);
        [Header("Gizmo를 그릴지의 여부")]
        [SerializeField] private bool drawGizmo;

        public bool IsGrounded;
        public bool IsSnapGround;

        private float stepOffset = 0.2f;
        private float stepMaxHeight;

        private Vector3 rayOrigin;
        private Vector3 rayEndPos;

        private void Start()
        {
            player = GetComponent<PlayerController>();
            stepOffset = player.PlayerCC.stepOffset;
            stepMaxHeight = stepOffset;
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmo || player == null) return;

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position + transform.up * 0.5f - transform.up * detectionMaxDist, boxHalfExtent * 2);

            rayOrigin = transform.position + (player.PlayerCC.velocity.normalized * stepOffset) + (Vector3.up * stepMaxHeight);
            rayEndPos = rayOrigin + Vector3.down * stepMaxHeight * 2;

            Gizmos.DrawLine(rayOrigin, rayEndPos);
            if (Physics.Linecast(rayOrigin, rayEndPos, out RaycastHit hitInfo, WorldManager.Instance.GetLevelLayers()))
            {
                Gizmos.DrawWireSphere(hitInfo.point, 0.1f);
            }
        }

        private void Update()
        {
            IsGrounded = CheckGround();
            IsSnapGround = CheckSnapGround();
        }

        private bool CheckGround()
        {
            bool result = Physics.BoxCast(
                transform.position + transform.up * 0.5f,
                boxHalfExtent,
                -transform.up,
                transform.rotation,
                detectionMaxDist,
                WorldManager.Instance.GetLevelLayers());
            if (!result)
            {
                return player.PlayerCC.isGrounded;
            }
            return result;
        }

        private bool CheckSnapGround()
        {
            rayOrigin = transform.position + (player.PlayerCC.velocity.normalized * stepOffset) + (Vector3.up * stepMaxHeight);
            rayEndPos = rayOrigin + Vector3.down * stepMaxHeight * 2;
            return Physics.Linecast(rayOrigin, rayEndPos, WorldManager.Instance.GetLevelLayers());
        }
    }
}
