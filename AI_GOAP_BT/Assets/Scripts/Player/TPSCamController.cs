using Unity.Cinemachine;
using UnityEngine;

public class TPSCamController : MonoBehaviour
{
    private Player.FSM.PlayerController player;

    public Camera MyLocalCamera;
    [SerializeField] private Transform camTarget;
    public Transform CamTarget { get { return camTarget; } }
    [SerializeField] private Transform chestTransform;
    public Transform ChestTransform { get { return chestTransform; } }

    public Camera MyCamera { get; private set; }
    public CinemachineBrain CamBrain { get; private set; }
    private CinemachineCamera normalCamera;
    private CinemachineCamera aimCamera;

    private float yRotation = 0;
    private float xRotation = 0;
    
    private void Awake()
    {
        player = GetComponent<Player.FSM.PlayerController>();

        MyCamera = Camera.main;
        CamBrain = MyCamera.GetComponent<CinemachineBrain>();

        normalCamera = MyCamera.transform.parent.Find("DefaultCamera").GetComponent<CinemachineCamera>();
        normalCamera.Follow = camTarget;
        normalCamera.LookAt = camTarget;

        aimCamera = MyCamera.transform.parent.Find("AimCamera").GetComponent<CinemachineCamera>();
        aimCamera.Follow = camTarget;
        aimCamera.LookAt = camTarget;

        camTarget.transform.parent = null;
        camTarget.rotation = transform.rotation;
    }

    public void Update()
    {
        camTarget.position = chestTransform.position;
        CamTargetRotate();
    }

    private void CamTargetRotate()
    {
        yRotation = CamTarget.transform.eulerAngles.y + player.InputMap.Input.YRotationEuler;
        xRotation = xRotation + player.InputMap.Input.XRotationEuler;
        xRotation = Mathf.Clamp(xRotation, -60, 60);

        CamTarget.transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
    }

    public float GetCameraRotaionY()
    {
        return MyCamera.transform.eulerAngles.y;
    }

    public void ActivateAimModeCam()
    {
        aimCamera.Prioritize();
    }

    public void DeactivateAimModeCam()
    {
        aimCamera.Priority = -1;
    }
}
