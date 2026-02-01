using System;
using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float damage);
}

public class Health : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    [Header("Feedback (optional)")]
    public HitReaction hitReaction;   // assign in inspector OR auto-find

    public event Action<float> OnDamaged;
    public event Action OnDied;

    void Awake()
    {
        CurrentHealth = maxHealth;
        if (!hitReaction) hitReaction = GetComponentInChildren<HitReaction>();
    }

    public void TakeDamage(float damage)
    {
        if (IsDead) return;
        if (damage <= 0f) return;

        CurrentHealth -= damage;
        OnDamaged?.Invoke(damage);

        // trigger visuals here (no longer from hitbox)
        if (hitReaction) hitReaction.Flash();

        if (CurrentHealth <= 0f)
        {
            CurrentHealth = 0f;
            IsDead = true;
            OnDied?.Invoke();
        }
    }
}
