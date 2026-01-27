using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using Unity.VisualScripting;

public class ActorUIMarker : MonoBehaviour
{
    public Image Marker;
    public TextMeshProUGUI Nickname;

    Vector3 baseImageScale;
    Vector3 worldPos;
    bool isAlly;
    bool disable = false;
    public void SetDisable() => disable = true;

    private static readonly RaycastHit[] rayHits = new RaycastHit[1];

    private void Awake()
    {
        Marker.enabled = false;
        Nickname.enabled = false;

        baseImageScale = Marker.rectTransform.localScale;
    }

    private void LateUpdate()
    {
        if (CameraManager.Instance.MainCam == null) return;
        if (disable)
        {
            Marker.enabled = false;
            Nickname.enabled = false;
            return;
        }

        worldPos = transform.position + Vector3.up * 1.75f;

        Billboard(CameraManager.Instance.MainCam, worldPos);
        ScaleUpdate();
    }

    private void FixedUpdate()
    {
        if (CameraManager.Instance.MainCam == null) return;
        if (disable) return;

        AlphaUpdate(CameraManager.Instance.MainCam, worldPos);
    }

    private void Billboard(Camera cam, Vector3 worldPos)
    {
        if (!isAlly)
        {
            Marker.enabled = false;
            Nickname.enabled = false;
            return;
        }

        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        if (screenPos.z <= 0f)
        {
            Marker.enabled = false;
            Nickname.enabled = false;
            return;
        }

        bool isOutside =
            screenPos.x < 0f || screenPos.x > Screen.width ||
            screenPos.y < 0f || screenPos.y > Screen.height;

        if (isOutside)
        {
            Marker.enabled = false;
            Nickname.enabled = false;
            return;
        }

        Marker.enabled = true;
        Nickname.enabled = true;
        Marker.transform.position = screenPos;
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

        Color imgColor = Marker.color;
        imgColor.a = alpha;

        Marker.color = imgColor;
        Nickname.color = imgColor;
    }

    private void ScaleUpdate()
    {
        float distance = Vector3.Distance(
            CameraManager.Instance.MainCam.transform.position,
            transform.position
        );

        float scaleFactor = 1f;

        if (distance > 20f)
        {
            float t = Mathf.InverseLerp(10f, 100f, distance);
            t = Mathf.Clamp01(t);
            scaleFactor = Mathf.Lerp(1f, 0.3f, t);
        }

        Marker.rectTransform.localScale = baseImageScale * scaleFactor;
    }

    public void SetColor(Team team)
    {
        if (WorldManager.Instance == null || disable) return;

        if (NetworkClient.localPlayer == null)
        {
            Marker.enabled = false;
            Nickname.enabled = false;
            return;
        }

        Team localPlayerTeam = Team.Blue;

        if (NetworkClient.localPlayer.TryGetComponent<Stat>(out Stat localStat))
        {
            localPlayerTeam = localStat.MyTeam;
        }
        else if (NetworkClient.localPlayer.TryGetComponent<LobbyPlayer>(out LobbyPlayer lobbyPlayer))
        {
            localPlayerTeam = lobbyPlayer.MyTeam;
        }
        else
        {
            Debug.LogWarning("LocalPlayer does not have Stat or LobbyPlayer component.");
            Marker.enabled = false;
            Nickname.enabled = false;
            return;
        }

        isAlly = localPlayerTeam == team;

        if (!isAlly)
        {
            Marker.enabled = false;
            Nickname.enabled = false;
            return;
        }

        Color color = team == Team.Blue
            ? WorldManager.Instance.BlueTeamColor
            : WorldManager.Instance.RedTeamColor;

        Marker.color = color;
        Nickname.color = color;
    }

    public void SetNickname(string name)
    {
        Nickname.text = name;
    }
}
