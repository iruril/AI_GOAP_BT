using UnityEngine;
using MEC;
using System.Collections.Generic;

public class GrazeListener : MonoBehaviour
{
    [SerializeField] private Transform owner;
    public Transform Owner => owner;

    [SerializeField] private float setCooldown = 2f;
    bool cooldown = false;

    public void OnGraze(Vector3 shotOrigin)
    {
        if (!cooldown)
        {
            if (owner.TryGetComponent<Stat>(out Stat stat))
            {
                Timing.RunCoroutine(ProhibitSet());
                stat.OnGraze(shotOrigin);
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
