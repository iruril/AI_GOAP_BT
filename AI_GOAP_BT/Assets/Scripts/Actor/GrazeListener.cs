using UnityEngine;
using MEC;
using System.Collections.Generic;

public class GrazeListener : MonoBehaviour
{
    [SerializeField] private Transform owner;

    [SerializeField] private float setCooldown = 2f;
    bool cooldown = false;

    public void OnGraze(Vector3 shotOrigin, LayerMask bulletOwnerLayer)
    {
        if (!cooldown)
        {
            if (owner.TryGetComponent<Stat>(out var stat))
            {
                Timing.RunCoroutine(ProhibitSet());
                stat.OnGraze(shotOrigin, bulletOwnerLayer);
            }
        }
    }

    private IEnumerator<float> ProhibitSet()
    {
        cooldown = true;
        yield return Timing.WaitForSeconds(setCooldown);
        cooldown = false;
    }
}
