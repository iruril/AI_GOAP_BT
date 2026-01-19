using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MEC;
using System.Collections.Generic;

public class CaptureHUDItem : MonoBehaviour
{
    [SerializeField] Image image;
    [SerializeField] TextMeshProUGUI text;
    [SerializeField] float colorLerpTime = 0.25f;

    public string CurrentText => text.text;
    CoroutineHandle colorRoutine;

    private void OnDestroy()
    {
        Timing.KillCoroutines(colorRoutine);
    }

    public void SetText(string s)
    {
        text.text = s;
    }

    public void SetColor(Color target)
    {
        Timing.KillCoroutines(colorRoutine);
        colorRoutine = Timing.RunCoroutine(ColorLerpRoutine(target));
    }

    private IEnumerator<float> ColorLerpRoutine(Color target)
    {
        float t = 0f;

        Color startText = text.color;
        Color startImage = image.color;

        while (t < colorLerpTime)
        {
            t += Time.deltaTime;
            float lerpT = Mathf.Clamp01(t / colorLerpTime);

            lerpT = lerpT * lerpT * lerpT * (lerpT * (6f * lerpT - 15f) + 10f); //5Â÷ SmoothStep

            text.color = Color.Lerp(startText, target, lerpT);
            image.color = Color.Lerp(startImage, target, lerpT);

            yield return Timing.WaitForOneFrame;
        }

        text.color = target;
        image.color = target;
    }

    public void SetFillAmout(float amount)
    {
        image.fillAmount = amount;
    }

    public void ResetContent()
    {
        SetText(string.Empty);
        SetColor(Color.white);
        SetFillAmout(0f);
    }
}
