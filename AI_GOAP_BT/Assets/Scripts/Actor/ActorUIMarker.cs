using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

[RequireComponent(typeof(Stat))]
public class ActorUIMarker : MonoBehaviour
{
    private Stat myActor;

    [SerializeField] Image marker;
    [SerializeField] TextMeshProUGUI nickname;

    Vector3 baseImageScale;
    Vector3 worldPos;
    bool isAlly;

    private static readonly RaycastHit[] rayHits = new RaycastHit[1];

    private void Awake()
    {
        myActor = GetComponent<Stat>();
    }

    private void Start()
    {
        myActor.OnTeamChange += SetColor; 
        marker.enabled = false;
        nickname.enabled = false;

        StartCoroutine(Init());
    }

    private void OnDestroy()
    {
        myActor.OnTeamChange -= SetColor;
        StopAllCoroutines();
    }

    private IEnumerator Init()
    {
        yield return new WaitUntil(() => GameManager.GetInstance().MyPlayer != null);

        SetColor(myActor.MyTeam);
        SetNickname(myActor.Nickname);
        baseImageScale = marker.rectTransform.localScale;
    }

    private void LateUpdate()
    {
        if(GameManager.GetInstance().MyPlayer == this.gameObject && this.enabled)
        {
            this.enabled = false;
            marker.enabled = false;
            nickname.enabled = false;
        }

        if (CameraManager.Instance.MainCam == null) return;

        worldPos = myActor.transform.position + Vector3.up * 1.75f;

        Billboard(CameraManager.Instance.MainCam, worldPos);
        ScaleUpdate();
    }

    private void FixedUpdate()
    {
        AlphaUpdate(CameraManager.Instance.MainCam, worldPos);
    }

    private void Billboard(Camera cam, Vector3 worldPos)
    {
        if (!isAlly)
        {
            marker.enabled = false;
            nickname.enabled = false;
            return;
        }

        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        if (screenPos.z <= 0f)
        {
            marker.enabled = false;
            nickname.enabled = false;
            return;
        }

        bool isOutside =
            screenPos.x < 0f || screenPos.x > Screen.width ||
            screenPos.y < 0f || screenPos.y > Screen.height;

        if (isOutside)
        {
            marker.enabled = false;
            nickname.enabled = false;
            return;
        }

        marker.enabled = true;
        nickname.enabled = true;
        marker.transform.position = screenPos;
    }

    private void AlphaUpdate(Camera cam, Vector3 worldPos)
    {
        Vector3 camPos = cam.transform.position;
        Vector3 targetPos = worldPos;
        Vector3 dir = targetPos - camPos;
        float dist = dir.magnitude;

        bool isOccluded = 0 < Physics.RaycastNonAlloc(
            camPos,
            dir.normalized,
            rayHits,
            dist,
            WorldManager.Instance.GetLevelLayers()
        );

        float alpha = isOccluded ? 0.5f : 1f;

        Color imgColor = marker.color;
        imgColor.a = alpha;

        marker.color = imgColor;
        nickname.color = imgColor;
    }

    private void ScaleUpdate()
    {
        float distance = Vector3.Distance(
            CameraManager.Instance.MainCam.transform.position,
            myActor.transform.position
        );

        float scaleFactor = 1f;

        if (distance > 20f)
        {
            float t = Mathf.InverseLerp(10f, 50f, distance);
            t = Mathf.Clamp01(t);
            scaleFactor = Mathf.Lerp(1f, 0.5f, t);
        }

        marker.rectTransform.localScale = baseImageScale * scaleFactor;
    }

    private void SetColor(Team team)
    {
        if (WorldManager.Instance == null) return;

        Team myTeam = GameManager.GetInstance().MyPlayer.GetComponent<Stat>().MyTeam;
        isAlly = myTeam == team;

        if (!isAlly)
        {
            marker.enabled = false;
            nickname.enabled = false;
            return;
        }

        Color color = team == Team.Blue 
            ? WorldManager.Instance.BlueTeamColor
            : WorldManager.Instance.RedTeamColor;

        marker.color = color;
        nickname.color = color;
    }

    private void SetNickname(string name)
    {
        nickname.text = name;
    }
}
