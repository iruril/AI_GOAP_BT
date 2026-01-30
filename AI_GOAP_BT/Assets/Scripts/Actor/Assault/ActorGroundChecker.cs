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

            if (CheckGround(out float dist))
            {
                ApplyGroundSnapping(dist);
                IsGrounded = true;
            }
            else
            {
                IsGrounded = false;
            }
        }

        private void FixedUpdate()
        {
        }

        private bool CheckGround(out float groundDist)
        {
            Vector3 origin = transform.position + Vector3.up * shellOffset;

            if (Physics.BoxCast(
                origin,
                boxHalfExtent,
                Vector3.down,
                out RaycastHit hit,
                transform.rotation,
                detectionRange + shellOffset,
                WorldManager.Instance.GetLevelLayers()
            ))
            {
                groundDist = hit.distance - shellOffset;
                return true;
            }

            groundDist = 0f;
            return false;
        }

        private void ApplyGroundSnapping(float dist)
        {
            if (ai.traversingOffMeshLink) return;

            if (dist > 0.01f && dist < maxSnapDistance)
            {
                Vector3 pos = ctx.MyRigid.position;
                ctx.MyRigid.MovePosition(pos + Vector3.down * dist);

                Vector3 vel = ctx.MyRigid.linearVelocity;
                if (vel.y < 0)
                {
                    vel.y = 0;
                    ctx.MyRigid.linearVelocity = vel;
                }
            }
        }
    }
}