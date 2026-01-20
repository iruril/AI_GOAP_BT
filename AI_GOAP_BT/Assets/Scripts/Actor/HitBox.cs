using RootMotion.FinalIK;
using UnityEngine;

public class HitBox : MonoBehaviour
{
    [SerializeField] private Collider col;
    [SerializeField] private CorpseGenerator corpseGenerator;
    [SerializeField] private HitReaction hitReactIK;
    [SerializeField] private Transform owner;

    public bool ApplyDamage(float dmg, Vector3 shotOrigin, Vector3 hitPoint, LayerMask friendLayer, bool isServer)
    {
        if ((friendLayer & (1 << owner.gameObject.layer)) != 0) return false;

        bool result = false;
        if (owner.TryGetComponent<IDamageable>(out var damageable))
        {
            if (isServer)
            {
                damageable.ApplyDamage(dmg, shotOrigin, hitPoint);
                corpseGenerator.LatestHittedPart = this.transform.name;
                result = true;
            }
            corpseGenerator.ShotOrigin = shotOrigin;
            Vector3 hitforce = (transform.position - shotOrigin).normalized;
            hitReactIK.Hit(col, hitforce, hitPoint);
        }
        return result;
    }

    public void SendShooterInfo(Transform attacker, bool isBlueTeam)
    {
        if (attacker.TryGetComponent<Stat>(out var attackerStat))
        {
            if (owner.TryGetComponent<Stat>(out var victimStat))
            {
                victimStat.SetAttacker(attackerStat.netId);
                victimStat.AddDmgContributer(attackerStat.netId);
            }
        }
    }

#if UNITY_EDITOR
    public void InitHitBox(Transform owner, CorpseGenerator corpseGenerator, HitReaction hitReactIK, int ownerLayer)
    {
        this.col = GetComponent<Collider>();
        this.owner = owner;
        this.corpseGenerator = corpseGenerator;
        this.hitReactIK = hitReactIK;
    }
#endif
}
