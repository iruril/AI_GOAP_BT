using UnityEngine;

public class Stat : MonoBehaviour, IDamageable
{
    [SerializeField] private float MaxHP = 100f;

    public float CurrentHP { get; private set; }
    public bool IsDead { get; private set; } = false;

    private void Awake()
    {
        CurrentHP = MaxHP;
    }

    #region Damageable Field
    public virtual void ApplyDamage(float dmg)
    {
        CurrentHP -= dmg;

        if (CurrentHP <= 0f)
        {
            CurrentHP = 0f;
            IsDead = true;
        }
    }
    #endregion
}
