using RootMotion.FinalIK;
using UnityEngine;
using Sound;

public class FootstepPlayer : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float groundCastLength = 0.15f; 
    [SerializeField] private float footStepMinimumGap = 0.1f;

    [SerializeField] private AudioSource audioSource; 
    private FullBodyBipedIK fbbik;

    private bool leftFootGrounded;
    private bool rightFootGrounded; 
    
    private float lastLeftFootStepTime;
    private float lastRightFootStepTime;

    void Start()
    {
        fbbik = GetComponent<FullBodyBipedIK>(); 
        if (audioSource != null)
        {
            audioSource.loop = false;
            audioSource.spatialBlend = 1.0f;
        }
    }

    void FixedUpdate()
    {
        if (fbbik == null) return;

        Vector3 leftFootPos = fbbik.references.leftFoot.position;
        Vector3 rightFootPos = fbbik.references.rightFoot.position;

        DetectFootstep(leftFootPos, ref leftFootGrounded, ref lastLeftFootStepTime);
        DetectFootstep(rightFootPos, ref rightFootGrounded, ref lastRightFootStepTime);
    }

    void DetectFootstep(Vector3 footPosition, ref bool wasGrounded, ref float lastStepTime)
    {
        bool isHit = Physics.Linecast(
            footPosition,
            footPosition + Vector3.down * groundCastLength,
            out RaycastHit hit,
            WorldManager.Instance.GetLevelLayers()
        );

        if (isHit)
        {
            if (!wasGrounded)
            {
                wasGrounded = true;

                if (Time.time - lastStepTime > footStepMinimumGap)
                {
                    FootStep();
                    lastStepTime = Time.time;
                }
            }
        }
        else
        {
            wasGrounded = false;
        }
    }

    public void FootStep()
    {
        if (audioSource != null) audioSource.pitch = Random.Range(0.95f, 1.05f);
        SoundManager.Instance.PlaySound("SFX_FootStep", audioSource);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (fbbik == null) fbbik = GetComponent<FullBodyBipedIK>();
        if (fbbik == null || fbbik.solver == null) return;

        DrawFootGizmo(fbbik.references.leftFoot.position, leftFootGrounded);
        DrawFootGizmo(fbbik.references.rightFoot.position, rightFootGrounded);
    }

    private void DrawFootGizmo(Vector3 pos, bool isGrounded)
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;

        Vector3 endPos = pos + Vector3.down * groundCastLength;
        Gizmos.DrawLine(pos, endPos);

        Gizmos.DrawWireSphere(endPos, 0.02f);
    }
#endif
}
