using Mirror;
using UnityEngine;

public interface IDamageable
{
    [Server]
    void ApplyDamage(float dmg, Vector3 shotOrigin, Vector3 hitPoint);
}
