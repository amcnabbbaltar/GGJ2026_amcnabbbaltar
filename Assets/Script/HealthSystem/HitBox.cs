using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Hitbox : MonoBehaviour
{
    [Header("Hit Settings")]
    public LayerMask targetLayers;
    public float damage = 10f;
    public float hitCooldown = 0.2f;

    bool active;
    float nextHitTime;
    Collider hitboxCollider;
    public bool activeOnStart = false;
    public bool destroyOnHit = false;
    void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
        if (activeOnStart)
            SetActive(true);
        else
        {
            hitboxCollider.isTrigger = true;
            SetActive(false);
        }

    }

    public void SetActive(bool value)
    {
        active = value;
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = value;
        }
        
    }

    void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryHit(other);
    }

    bool TryHit(Collider other)
    {
        if (!active) return false;
        if (Time.time < nextHitTime) return false;

        if (((1 << other.gameObject.layer) & targetLayers) == 0)
            return false;
        // Look for a damageable component on the target or its parents
        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null) return false;

        damageable.TakeDamage(damage);
        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
        nextHitTime = Time.time + hitCooldown;
        return true;
    }
}
