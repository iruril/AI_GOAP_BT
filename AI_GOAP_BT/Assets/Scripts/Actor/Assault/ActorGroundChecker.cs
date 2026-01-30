using UnityEngine;
using Pathfinding;
using Mirror;

namespace AnimControl
{
    public class AIGroundChecker : NetworkBehaviour
    {
        private Assault.AssaultAnimFSM ctx;
        private RichAI ai;

        [Header("Detection Settings")]
        [SerializeField] private float detectionRange = 0.3f;
        [SerializeField] private float shellOffset = 0.1f;
        [SerializeField] private Vector3 boxHalfExtent = new Vector3(0.3f, 0.05f, 0.3f);

        [Header("Snap Settings")]
        [SerializeField] private float maxSnapDistance = 0.5f;

        public bool IsGrounded { get; private set; }
        private RaycastHit[] groundHits = new RaycastHit[1];

        private void Awake()
        {
            ctx = GetComponent<Assault.AssaultAnimFSM>();
            ai = GetComponent<RichAI>();
        }

        public override void OnStartServer()
        {
            enabled = true;
        }

        public override void OnStartClient()
        {
            if (isServer) return;
            enabled = false;
        }

        private void Update()
        {
            if (!isServer) return;
            IsGrounded = CheckGround();
            if (IsGrounded)
            {
                ApplyGroundSnapping();
            }
        }

        private void FixedUpdate()
        {
            if (!isServer) return;
        }

        private bool CheckGround()
        {
            Vector3 origin = transform.position + Vector3.up * shellOffset;

            int count = Physics.BoxCastNonAlloc(
                origin,
                boxHalfExtent,
                Vector3.down,
                groundHits,
                transform.rotation,
                detectionRange + shellOffset,
                WorldManager.Instance.GetLevelLayers()
            );

            return count > 0;
        }

        private void ApplyGroundSnapping()
        {
            if (ai.traversingOffMeshLink) return;

            float dist = groundHits[0].distance - shellOffset;

            if (dist > 0.01f && dist < maxSnapDistance)
            {
                Vector3 currentPos = ctx.MyRigid.position;
                Vector3 snapDelta = Vector3.down * dist;
                ctx.MyRigid.MovePosition(currentPos + snapDelta);

                Vector3 vel = ctx.MyRigid.linearVelocity;
                if (vel.y < 0)
                {
                    vel.y = 0;
                    ctx.MyRigid.linearVelocity = vel;
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            Gizmos.color = IsGrounded ? Color.cyan : Color.red;
            Vector3 center = transform.position + Vector3.up * shellOffset + Vector3.down * groundHits[0].distance;
            Gizmos.DrawWireCube(center, boxHalfExtent * 2);
        }
    }
}