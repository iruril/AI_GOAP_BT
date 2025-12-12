using RootMotion.FinalIK;
using UnityEngine;

public class HitBox : MonoBehaviour
{
    [SerializeField] private Collider col;
    [SerializeField] private CorpseGenerator corpseGenerator;
    [SerializeField] private HitReaction hitReactIK;
    [SerializeField] private Transform owner;
    [SerializeField] private int ownerLayer;

    public void ApplyDamage(float dmg, Vector3 shotOrigin, Vector3 hitPoint, LayerMask friendLayer, bool isServer)
    {
        if ((friendLayer & (1 << ownerLayer)) != 0) return;

        if (owner.TryGetComponent<IDamageable>(out var damageable))
        {
            if (isServer)
            {
                damageable.ApplyDamage(dmg, shotOrigin, hitPoint);
            }
            corpseGenerator.LatestHittedPart = this.transform.name;
            corpseGenerator.ShotOrigin = shotOrigin;
            Vector3 hitforce = (transform.position - shotOrigin).normalized;
            hitReactIK.Hit(col, hitforce, hitPoint);
        }
    }

#if UNITY_EDITOR
    public void InitHitBox(Transform owner, CorpseGenerator corpseGenerator, HitReaction hitReactIK, int ownerLayer)
    {
        this.col = GetComponent<Collider>();
        this.owner = owner;
        this.corpseGenerator = corpseGenerator;
        this.hitReactIK = hitReactIK;
        this.ownerLayer = ownerLayer;
    }
#endif
}
