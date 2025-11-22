using UnityEngine;

public interface IDamageable
{
    void ApplyDamage(float dmg, Vector3 shotOrigin, Vector3 hitPoint);
}
