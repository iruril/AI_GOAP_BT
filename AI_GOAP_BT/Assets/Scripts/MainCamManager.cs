using MEC;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class MainCamManager : MonoBehaviour
{
    public static MainCamManager Instance = null;

    [SerializeField] Camera cam;
    public Camera MainCam { get { return cam; } }
    [SerializeField] CinemachineCamera defaultCam;
    [SerializeField] CinemachineCamera aimCam;

    CinemachineThirdPersonFollow defaultCamFollow;
    CinemachineThirdPersonFollow aimCamFollow;

    CoroutineHandle leanHandle;

    [Header("카메라 좌/우 전환")]
    [SerializeField]
    private AnimationCurve leanCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); 
    [SerializeField] private float leanDuration = 0.1f;

    private bool isleanLeft = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(this.gameObject);
            return;
        }
        defaultCamFollow = defaultCam.GetComponent<CinemachineThirdPersonFollow>();
        aimCamFollow = aimCam.GetComponent<CinemachineThirdPersonFollow>();
    }

    private void OnDestroy()
    {
        Timing.KillCoroutines(leanHandle);
    }

    private void OnDisable()
    {
        Timing.KillCoroutines(leanHandle);
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
        aimCam.Priority = 100;
        defaultCam.Priority = 0;
    }

    public void ActivateDefaultModeCam()
    {
        aimCam.Priority = 0;
        defaultCam.Priority = 100;
    }

    public void Lean(bool isLeft)
    {
        if (isLeft == isleanLeft) return;

        isleanLeft = isLeft;

        float targetSide = isLeft ? 0f : 1f;

        Timing.KillCoroutines(leanHandle);
        leanHandle = Timing.RunCoroutine(CoLean(targetSide));
    }

    private IEnumerator<float> CoLean(float targetSide)
    {
        float duration = leanDuration;
        float time = 0f;

        float startDefault = defaultCamFollow.CameraSide;
        float startAim = aimCamFollow.CameraSide;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            float easedT = leanCurve.Evaluate(t);
            float side = Mathf.Lerp(startDefault, targetSide, easedT);

            defaultCamFollow.CameraSide = side;
            aimCamFollow.CameraSide = side;

            yield return Timing.WaitForOneFrame;
        }

        defaultCamFollow.CameraSide = targetSide;
        aimCamFollow.CameraSide = targetSide;
    }
}
