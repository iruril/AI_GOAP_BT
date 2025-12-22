using Mirror;
using UnityEngine;

namespace Player.FSM
{
    public class GroundChecker : MonoBehaviour
    {
        private PlayerController player;
        private Rigidbody rb;

        [Header("Boxcast로 감지할 최대 거리")]
        [SerializeField] private float detectionMaxDist;
        [Header("Boxcast 시 박스 사이즈")]
        [SerializeField] private Vector3 boxHalfExtent = new Vector3(0.5f, 0.3f, 0.5f);
        [Header("Gizmo를 그릴지의 여부")]
        [SerializeField] private bool drawGizmo;

        public bool IsGrounded;
        public bool IsSnapGround;

        private float stepOffset = 0.3f;
        private float stepMinDepth;
        private float stepMaxHeight;

        private Vector3 rayOrigin;
        private Vector3 rayEndPos;

        private const float STEP_HEIGHT_ERROR = 0.3f;

        private void Start()
        {
            player = GetComponent<PlayerController>();
            rb = GetComponent<Rigidbody>();
            stepMaxHeight = stepOffset + STEP_HEIGHT_ERROR;
            stepMinDepth = stepOffset;
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmo) return; 
            
            Debug.DrawRay(transform.position, transform.forward * 2f, Color.red);
            Debug.DrawRay(transform.position, transform.right * 2f, Color.blue);

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position + transform.up * 0.5f - transform.up * detectionMaxDist, boxHalfExtent * 2);

            if (player != null)
            {
                rayOrigin = transform.position + (rb.linearVelocity.normalized * stepMinDepth) + (Vector3.up * stepMaxHeight);
                rayEndPos = rayOrigin + Vector3.down * stepMaxHeight * 2;

                Gizmos.DrawLine(rayOrigin, rayEndPos);
                if (Physics.Linecast(rayOrigin, rayEndPos, out RaycastHit hitInfo, WorldManager.Instance.GetLevelLayers()))
                {
                    Gizmos.DrawWireSphere(hitInfo.point, 0.1f);
                }
            }
        }

        private void FixedUpdate()
        {
            IsGrounded = CheckGround();
            IsSnapGround = CheckSnapGround();
        }

        private bool CheckGround()
        {
            bool result = Physics.BoxCast(transform.position + transform.up * 0.5f, boxHalfExtent, -transform.up, transform.rotation, detectionMaxDist, WorldManager.Instance.GetLevelLayers());
            return result;
        }

        private bool CheckSnapGround()
        {
            rayOrigin = transform.position + (rb.linearVelocity.normalized * stepMinDepth) + (Vector3.up * stepMaxHeight);
            rayEndPos = rayOrigin + Vector3.down * stepMaxHeight * 2;
            return Physics.Linecast(rayOrigin, rayEndPos, WorldManager.Instance.GetLevelLayers());
        }
    }
}
