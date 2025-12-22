using Unity.Cinemachine;
using UnityEngine;

public class TPSCamController : MonoBehaviour
{
    private Player.FSM.PlayerController player;

    [SerializeField] private Transform camTarget;
    public Transform CamTarget { get { return camTarget; } }

    private float yRotation = 0;
    private float xRotation = 0;
    
    private void Awake()
    {
        player = GetComponent<Player.FSM.PlayerController>();
    }

    public void InitCam()
    {
        MainCamManager.Instance.SetCamTarget(CamTarget);

        camTarget.transform.parent = null;
        camTarget.rotation = transform.rotation;
    }

    public void Update()
    {
        if (!player.isLocalPlayer) return;

        camTarget.position = transform.position + Vector3.up;
        CamTargetRotate();
    }

    private void CamTargetRotate()
    {
        yRotation = CamTarget.transform.eulerAngles.y + player.InputMap.Input.YRotationEuler;
        xRotation = xRotation + player.InputMap.Input.XRotationEuler;
        xRotation = Mathf.Clamp(xRotation, -60, 60);

        CamTarget.transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
    }
}
