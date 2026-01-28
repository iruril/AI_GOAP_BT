using UnityEngine;
using TMPro;
using System;

public class DamageStackItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI damageText;
    private uint targetId;
    private float currentDamage;
    private float expireTime; 
    private float totalDuration;
    private Action<uint> onExpired;

    public void Init(uint id, float initialDmg, float duration, Action<uint> expiryCallback, bool isKilled = false)
    {
        targetId = id;
        currentDamage = 0f;
        onExpired = expiryCallback;
        totalDuration = duration;

        Refresh(initialDmg, duration, isKilled);
    }

    public void Refresh(float addDmg, float duration, bool isKilled = false)
    {
        currentDamage += addDmg;
        damageText.text = Mathf.FloorToInt(currentDamage).ToString();

        damageText.color = isKilled ? Color.red : Color.white;
        transform.localScale = isKilled ? Vector3.one * 1.5f : Vector3.one * 1.25f;

        expireTime = Time.time + duration;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, Time.deltaTime * 5f);

        float remainingTime = expireTime - Time.time;
        float ratio = Mathf.Clamp01(remainingTime / totalDuration);

        damageText.alpha = ratio * ratio * ratio;

        if (remainingTime <= 0)
        {
            onExpired?.Invoke(targetId);
            gameObject.SetActive(false);
        }
    }
}