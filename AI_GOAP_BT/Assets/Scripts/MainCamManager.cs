using Unity.Cinemachine;
using UnityEngine;

public class MainCamManager : MonoBehaviour
{
    [SerializeField] Camera cam;
    [SerializeField] CinemachineCamera defaultCam;
    [SerializeField] CinemachineCamera aimCam;

    public static MainCamManager Instance = null;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(this.gameObject);
            return;
        }
    }

    public void SetCamTarget(Transform target)
    {
        defaultCam.Target.TrackingTarget = target;
        aimCam.Target.TrackingTarget = target;
    }

    public float GetCameraRotaionY()
    {
        return cam.transform.eulerAngles.y;
    }

    public void ActivateAimModeCam()
    {
        aimCam.Prioritize();
    }

    public void ActivateDefaultModeCam()
    {
        defaultCam.Prioritize();
    }
}
