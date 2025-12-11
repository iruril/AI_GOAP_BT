using Unity.Cinemachine;
using UnityEngine;

public class MainCamManager : MonoBehaviour
{
    [SerializeField] CinemachineCamera mainCam;

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
        mainCam.Target.TrackingTarget = target;
    }
}
