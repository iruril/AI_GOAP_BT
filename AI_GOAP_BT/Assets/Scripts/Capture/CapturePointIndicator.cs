using UnityEngine;
using TMPro;
using UnityEngine.UI;
using MEC;
using System.Collections.Generic;

namespace CapturePoint
{
    public class CapturePointIndicator : MonoBehaviour
    {
        private CapturePoint point;

        [SerializeField] Image pointImage;
        [SerializeField] TextMeshProUGUI pointName;
        [SerializeField] float colorLerpTime = 0.25f;

        CoroutineHandle colorRoutine; 
        
        Vector3 baseImageScale;
        Vector3 baseTextScale;

        private static readonly RaycastHit[] rayHits = new RaycastHit[1];

        Vector3 worldPos;

        private void Awake()
        {
            point = GetComponent<CapturePoint>();
        }

        private void Start()
        {
            pointName.text = point.CaptureName;

            point.OnColorChanged += SetColor;
            point.OnGaugeChanged += SetFillAmount;

            pointName.color = WorldManager.Instance.DefColor;
            pointImage.color = WorldManager.Instance.DefColor;
            SetFillAmount(0f);

            baseImageScale = pointImage.rectTransform.localScale;
            baseTextScale = pointName.rectTransform.localScale;
        }

        private void OnDestroy()
        {
            Timing.KillCoroutines(colorRoutine);
        }

        private void LateUpdate()
        {
            if (CameraManager.Instance.MainCam == null) return;

            worldPos = point.transform.position + Vector3.up * 5f;

            Billboard(CameraManager.Instance.MainCam, worldPos);
        }

        private void FixedUpdate()
        {
            AlphaUpdate(CameraManager.Instance.MainCam, worldPos);
            ScaleUpdate();
        }

        private void Billboard(Camera cam, Vector3 worldPos)
        {
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

            if (screenPos.z <= 0f)
            {
                pointImage.enabled = false;
                pointName.enabled = false;
                return;
            }

            bool isOutside =
                screenPos.x < 0f || screenPos.x > Screen.width ||
                screenPos.y < 0f || screenPos.y > Screen.height;

            if (isOutside)
            {
                pointImage.enabled = false;
                pointName.enabled = false;
                return;
            }

            pointImage.enabled = true;
            pointName.enabled = true;
            pointImage.transform.position = screenPos;
            pointName.transform.position = screenPos;
        }

        private void ScaleUpdate()
        {
            float distance = Vector3.Distance(
                CameraManager.Instance.MainCam.transform.position,
                point.transform.position
            );

            float scaleFactor = 1f;

            if (distance > 20f)
            {
                float t = Mathf.InverseLerp(10f, 100f, distance);
                t = Mathf.Clamp01(t);
                scaleFactor = Mathf.Lerp(1f, 0.3f, t);
            }

            pointImage.rectTransform.localScale = baseImageScale * scaleFactor;
            pointName.rectTransform.localScale = baseTextScale * scaleFactor;
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

            Color imgColor = pointImage.color;
            Color textColor = pointName.color;

            imgColor.a = alpha;
            textColor.a = alpha;

            pointImage.color = imgColor;
            pointName.color = textColor;
        }

        private void SetColor(Color color)
        {
            Timing.KillCoroutines(colorRoutine);
            colorRoutine = Timing.RunCoroutine(ColorLerpRoutine(color));
        }

        private void SetFillAmount(float amount)
        {
            pointImage.fillAmount = amount;
        }

        private IEnumerator<float> ColorLerpRoutine(Color target)
        {
            float t = 0f;

            Color startText = pointName.color;
            Color startImage = pointImage.color;

            while (t < colorLerpTime)
            {
                t += Time.deltaTime;
                float lerpT = Mathf.Clamp01(t / colorLerpTime);

                lerpT = lerpT * lerpT * lerpT * (lerpT * (6f * lerpT - 15f) + 10f); //5Â÷ SmoothStep

                pointName.color = Color.Lerp(startText, target, lerpT);
                pointImage.color = Color.Lerp(startImage, target, lerpT);

                yield return Timing.WaitForOneFrame;
            }

            pointName.color = target;
            pointImage.color = target;
        }
    }
}
