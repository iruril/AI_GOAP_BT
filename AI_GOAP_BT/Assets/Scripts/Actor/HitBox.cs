using UnityEngine;

public class HitBox : MonoBehaviour
{
    [SerializeField] private Transform owner;
    [SerializeField] private int ownerLayer;

    public void ApplyDamage(float dmg, Vector3 shotOrigin, Vector3 hitPoint, LayerMask friendLayer)
    {
        if ((friendLayer & (1 << ownerLayer)) != 0) return;

        if (owner.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.ApplyDamage(dmg, shotOrigin, hitPoint);
        }
    }

    public void InitHitBox(Transform owner, int ownerLayer)
    {
        this.owner = owner;
        this.ownerLayer = ownerLayer;
    }
}
