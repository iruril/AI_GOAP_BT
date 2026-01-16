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

        private const float STEP_HEIGHT_ERROR = 0.2f;

        private void Start()
        {
            player = GetComponent<PlayerController>();
            stepOffset = player.PlayerCC.stepOffset;
            stepMaxHeight = stepOffset + STEP_HEIGHT_ERROR;
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmo || player == null) return;

            Vector3 groundCenter =
                transform.position + Vector3.up * (player.PlayerCC.height * 0.5f - player.PlayerCC.radius);

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(
                groundCenter - Vector3.up * detectionMaxDist,
                boxHalfExtent * 2
            );

            Vector3 horizontalVel = player.PlayerCC.velocity;
            horizontalVel.y = 0f;

            Vector3 moveDir = horizontalVel.sqrMagnitude > 0.001f
                ? horizontalVel.normalized
                : transform.forward;

            Vector3 snapRayOrigin =
                transform.position + moveDir * stepOffset + Vector3.up * stepMaxHeight;

            Vector3 snapRayEnd =
                snapRayOrigin + Vector3.down * stepMaxHeight * 2f;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(snapRayOrigin, snapRayEnd);

            if (Physics.Linecast(
                snapRayOrigin,
                snapRayEnd,
                out RaycastHit hit,
                WorldManager.Instance.GetLevelLayers()))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(hit.point, 0.08f);
            }
        }

        private void Update()
        {
            IsGrounded = CheckGround();
            IsSnapGround = CheckSnapGround();
        }

        private bool CheckGround()
        {
            Vector3 center = transform.position + Vector3.up * (player.PlayerCC.height * 0.5f - player.PlayerCC.radius);

            bool result = Physics.BoxCast(
                center,
                boxHalfExtent,
                -transform.up,
                transform.rotation,
                detectionMaxDist,
                WorldManager.Instance.GetLevelLayers());
            if (!result)
            {
                result = player.PlayerCC.isGrounded;
            }
            return result;
        }

        private bool CheckSnapGround()
        {
            Vector3 horizontalVel = player.PlayerCC.velocity;
            horizontalVel.y = 0f;

            Vector3 moveDir = horizontalVel.sqrMagnitude > 0.001f
                ? horizontalVel.normalized
                : transform.forward;

            rayOrigin = transform.position + moveDir * stepOffset + Vector3.up * stepMaxHeight;

            rayEndPos = rayOrigin + Vector3.down * stepMaxHeight * 2;
            return Physics.Linecast(rayOrigin, rayEndPos, WorldManager.Instance.GetLevelLayers());
        }
    }
}
