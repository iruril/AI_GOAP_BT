using RootMotion.FinalIK;
using UnityEngine;

public class HitBox : MonoBehaviour
{
    public enum HitBoxType
    {
        Head,
        Body,
        Limb
    }

    [SerializeField] private Collider col;
    [SerializeField] private CorpseGenerator corpseGenerator;
    [SerializeField] private HitReaction hitReactIK;
    [SerializeField] private Transform owner;

    [SerializeField] private HitBoxType hitBoxType;

    private const float LIMB_DAMAGE_MULTIPLIER = 0.5f;
    private const float BODY_DAMAGE_MULTIPLIER = 1.0f;

    public void ApplyDamage(float dmg, float headMultiplier, Vector3 shotOrigin, Vector3 hitPoint, LayerMask friendLayer, Transform attacker, bool isBlueTeam, string gunName)
    {
        if ((friendLayer & (1 << owner.gameObject.layer)) != 0) return;

        if (attacker != null && owner.TryGetComponent<IDamageable>(out var damageable))
        {
            float finalDmg = dmg * (hitBoxType switch
            {
                HitBoxType.Head => headMultiplier,
                HitBoxType.Limb => LIMB_DAMAGE_MULTIPLIER,
                _ => BODY_DAMAGE_MULTIPLIER
            });

            if (attacker != null) UpdateDamageRecord(attacker, finalDmg, gunName);
            damageable.ApplyDamage(finalDmg, shotOrigin, hitPoint);
            corpseGenerator.LatestHittedPart = this.transform.name;
        }

        PlayHitEffects(shotOrigin, hitPoint);
    }

    private void UpdateDamageRecord(Transform attacker, float damage, string gunName)
    {
        if (attacker.TryGetComponent<Stat>(out var attackerStat))
        {
            if (owner.TryGetComponent<Stat>(out var victimStat))
            {
                victimStat.AddDamageRecord(attackerStat.netId, damage, hitBoxType, gunName);
            }
        }
    }

    private void PlayHitEffects(Vector3 origin, Vector3 point)
    {
        corpseGenerator.ShotOrigin = origin;
        Vector3 force = (transform.position - origin).normalized;
        if (hitReactIK != null) hitReactIK.Hit(col, force, point);
    }

#if UNITY_EDITOR
    public void InitHitBox(Transform owner, CorpseGenerator corpseGenerator, HitReaction hitReactIK, int ownerLayer, HitBoxType hitBoxType)
    {
        this.col = GetComponent<Collider>();
        this.owner = owner;
        this.corpseGenerator = corpseGenerator;
        this.hitReactIK = hitReactIK;
        this.hitBoxType = hitBoxType;
    }
#endif
}
