using UnityEngine;

public class HitBox : MonoBehaviour
{
    [SerializeField] private CorpseGenerator corpseGenerator;
    [SerializeField] private Transform owner;
    [SerializeField] private int ownerLayer;

    public void ApplyDamage(float dmg, Vector3 shotOrigin, Vector3 hitPoint, LayerMask friendLayer)
    {
        if ((friendLayer & (1 << ownerLayer)) != 0) return;

        if (owner.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.ApplyDamage(dmg, shotOrigin, hitPoint);
            corpseGenerator.LatestHittedPart = this.transform.name;
            corpseGenerator.ShotOrigin = shotOrigin;
        }
    }

    public void InitHitBox(Transform owner, CorpseGenerator corpseGenerator, int ownerLayer)
    {
        this.owner = owner;
        this.corpseGenerator = corpseGenerator;
        this.ownerLayer = ownerLayer;
    }
}
