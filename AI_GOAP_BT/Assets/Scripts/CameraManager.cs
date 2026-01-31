using MEC;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance = null;

    private Transform camTarget;
    private Stat targetStat;

    [SerializeField] Camera cam;
    public Camera MainCam { get { return cam; } }

    [SerializeField] CinemachineCamera defaultCam;
    [SerializeField] CinemachineCamera aimCam;
    [SerializeField] CinemachineCamera deadCam; 
    [SerializeField] private CinemachineImpulseSource impulseSource;

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
        Instance = this;

        defaultCamFollow = defaultCam.GetComponent<CinemachineThirdPersonFollow>();
        aimCamFollow = aimCam.GetComponent<CinemachineThirdPersonFollow>();
    }

    private void OnDestroy()
    {
        Timing.KillCoroutines(leanHandle);

        Instance = null;
    }

    public void SetDeadCamTarget(Corpse corpse)
    {
        deadCam.Target.TrackingTarget = corpse.Hip;
        deadCam.Target.LookAtTarget = corpse.Hip;
    }

    public void SetCamTarget(Transform target)
    {
        camTarget = target;
        defaultCam.Target.TrackingTarget = target;
        aimCam.Target.TrackingTarget = target;
    }

    public void SetTargetStat(Stat target)
    {
        targetStat = target;

        targetStat.OnRevive += OnRevive;
        targetStat.OnDead += OnDead;
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

    private void OnDead()
    {
        deadCam.Prioritize();
    }

    private void OnRevive()
    {
        ActivateDefaultModeCam();
        Lean(false);

        camTarget.parent.position = targetStat.SpawnPosition + Vector3.up;
        camTarget.parent.rotation = targetStat.SpawnRotation;

        TeleportAllCamera(camTarget.position);
    }

    public void TeleportAllCamera(Vector3 teleportTo)
    {
        TeleportCamera(defaultCam, teleportTo);
        TeleportCamera(aimCam, teleportTo);
        TeleportCamera(deadCam, teleportTo);
    }

    private void TeleportCamera(CinemachineCamera virtualCamera, Vector3 teleportTo)
    {
        Transform target = virtualCamera.Target.TrackingTarget;
        var delta = teleportTo - target.position;

        virtualCamera.OnTargetObjectWarped(target, delta);
        virtualCamera.PreviousStateIsValid = false;
    }

    public void PlayImpulse()
    {
        if (impulseSource == null) return;
        impulseSource.GenerateImpulseWithForce(1.0f);
    }
}
