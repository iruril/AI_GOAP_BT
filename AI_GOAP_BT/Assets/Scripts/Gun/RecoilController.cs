using UnityEngine;

public class RecoilController : MonoBehaviour
{
    private Vector3 currentRotation;
    private Vector3 targetRotation;

    private Vector3 currentVelocity;

    private float recoilPitch;
    private float recoilYaw;
    private float recoilRoll;

    [SerializeField] private float returnDamping = 12f;
    [SerializeField, Range(0f, 1f)] private float firingRecoveryScale = 0.1f;
    [SerializeField] private float recoilLifeTime = 0.05f;

    [SerializeField] private float snapTime = 0.03f;
    [SerializeField] private float maxPitch = 45f;
    [SerializeField] private float maxRoll = 3f;

    float lastApplyTime = 0f;

    void Update()
    {
        float damping = returnDamping;

        if (Time.time - lastApplyTime < recoilLifeTime)
        {
            Debug.Log("True");
            damping *= firingRecoveryScale;
        }

        float decay = Mathf.Exp(-damping * Time.deltaTime);
        targetRotation *= decay;

        currentRotation = Vector3.SmoothDamp(currentRotation, targetRotation, ref currentVelocity, snapTime);

        transform.localRotation = Quaternion.Euler(currentRotation);
    }

    public void ApplyRecoil()
    {
        targetRotation.x = Mathf.Clamp(targetRotation.x + recoilPitch, -maxPitch, maxPitch);
        targetRotation.y += Random.Range(-recoilYaw, recoilYaw);
        targetRotation.z = Mathf.Clamp(targetRotation.z + Random.Range(-recoilRoll, recoilRoll), -maxRoll, maxRoll);

        lastApplyTime = Time.time;
    }

    public void SetRecoilValue(float pitch, float yaw, float roll)
    {
        recoilPitch = pitch;
        recoilYaw = yaw;
        recoilRoll = roll;
    }
}
