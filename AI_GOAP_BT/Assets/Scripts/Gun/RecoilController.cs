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
    [SerializeField, Range(0f, 0.5f)] private float recoilLifeTime = 0.15f;

    [SerializeField] private float snapTime = 0.03f;
    [SerializeField] private float maxPitch = 45f;
    [SerializeField] private float maxRoll = 3f;

    bool isFiring = false;
    float lastApplyTime = 0f;

    void Update()
    {
        float damping = returnDamping;
        isFiring = Time.time - lastApplyTime < recoilLifeTime;

        if (isFiring)
        {
            damping *= firingRecoveryScale;
        }

        float decay = Mathf.Exp(-damping * Time.deltaTime);
        targetRotation *= decay;

        currentRotation = Vector3.SmoothDamp(currentRotation, targetRotation, ref currentVelocity, snapTime);

        transform.localRotation = Quaternion.Euler(currentRotation);
    }

    public float ConsumePitch(float inputPitch)
    {
        if (Mathf.Abs(currentRotation.x) < 0.01f)
            return inputPitch;

        bool isOpposite = (inputPitch < 0 && currentRotation.x > 0) || (inputPitch > 0 && currentRotation.x < 0);
        
        if (!isOpposite)
            return inputPitch;

        float cancelAmount = Mathf.Min(
            Mathf.Abs(inputPitch),
            Mathf.Abs(currentRotation.x)
        );

        cancelAmount *= Mathf.Sign(inputPitch);

        targetRotation.x += cancelAmount;
        currentRotation.x += cancelAmount;

        return inputPitch - cancelAmount;
    }

    public float ConsumeYaw(float inputYaw)
    {
        if (Mathf.Abs(currentRotation.y) < 0.01f)
            return inputYaw;

        bool isOpposite = (inputYaw < 0 && currentRotation.y > 0) || (inputYaw > 0 && currentRotation.y < 0);

        if (!isOpposite)
            return inputYaw;

        float cancelAmount = Mathf.Min(
            Mathf.Abs(inputYaw),
            Mathf.Abs(currentRotation.y)
        );

        cancelAmount *= Mathf.Sign(inputYaw);

        targetRotation.y += cancelAmount;
        currentRotation.y += cancelAmount;

        return inputYaw - cancelAmount;
    }

    public void ApplyRecoil()
    {
        targetRotation.x = Mathf.Clamp(targetRotation.x + recoilPitch, -maxPitch, maxPitch);
        targetRotation.y += Random.Range(-recoilYaw * 0.25f, recoilYaw);
        targetRotation.z = Mathf.Clamp(targetRotation.z + Random.Range(-recoilRoll, recoilRoll), -maxRoll, maxRoll);

        lastApplyTime = Time.time;
    }

    public void SetRecoilValue(float pitch, float yaw, float roll)
    {
        recoilPitch = pitch;
        recoilYaw = yaw;
        recoilRoll = roll;
    }

    public void ResetRecoil()
    {
        targetRotation = Vector3.zero;
        currentRotation = Vector3.zero;

        transform.localRotation = Quaternion.identity;
        transform.localPosition = Vector3.zero;
    }
}
