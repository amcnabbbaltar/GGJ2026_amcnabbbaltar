using UnityEngine;

public class MaskExplosion : MonoBehaviour
{
    [Header("Explosion")]
    public float radius = 3f;
    public float damage = 25f;
    public LayerMask damageLayers = ~0;
    public bool useFalloff = true;

    [Header("Knockback")]
    public bool applyKnockback = true;
    public float knockbackForce = 8f;

    [Header("VFX")]
    public GameObject explosionVfxPrefab;
    public float vfxDestroyAfter = 2f;

    [Header("SFX")]
    public AudioClip explosionSfx;
    [Range(0f, 1f)] public float volume = 1f;
    public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    [Header("Safety")]
    public float spawnOffsetUp = 0.05f;
    public bool destroyMaskOnExplode = true;

    bool exploded;
    OrbitingMask orbitingMask;

    void Awake()
    {
        orbitingMask = GetComponent<OrbitingMask>();
    }

    void OnCollisionEnter(Collision collision)
    {
        TryExplode(collision.collider);
    }

    void OnTriggerEnter(Collider other)
    {
        TryExplode(other);
    }

    void TryExplode(Collider hit)
    {
        if (exploded) return;

        // Only explode once thrown
        if (orbitingMask != null && !orbitingMask.IsThrown)
            return;

        exploded = true;

        Vector3 pos = transform.position + Vector3.up * spawnOffsetUp;

        // ───── VFX ─────
        if (explosionVfxPrefab)
        {
            var vfx = Instantiate(explosionVfxPrefab, pos, Quaternion.identity);
            Destroy(vfx, vfxDestroyAfter);
        }

        // ───── SFX ─────
        PlayExplosionSound(pos);

        // ───── DAMAGE ─────
        var hits = Physics.OverlapSphere(pos, radius, damageLayers, QueryTriggerInteraction.Ignore);

        foreach (var col in hits)
        {
            if (!col) continue;

            var hp = col.GetComponentInParent<Health>();
            if (hp == null || hp.IsDead) continue;

            float finalDamage = damage;

            if (useFalloff)
            {
                float d = Vector3.Distance(pos, col.ClosestPoint(pos));
                float t = Mathf.Clamp01(d / Mathf.Max(0.01f, radius));
                finalDamage = Mathf.Lerp(damage, 0f, t);
            }

            if (finalDamage > 0f)
                hp.TakeDamage(finalDamage);

            if (applyKnockback)
            {
                var rb = col.attachedRigidbody;
                if (rb != null)
                {
                    Vector3 dir = rb.worldCenterOfMass - pos;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.001f)
                        rb.AddForce(dir.normalized * knockbackForce, ForceMode.Impulse);
                }
            }
        }

        if (destroyMaskOnExplode)
            Destroy(gameObject);
    }

    void PlayExplosionSound(Vector3 position)
    {
        if (!explosionSfx) return;

        GameObject audioObj = new GameObject("MaskExplosion_SFX");
        audioObj.transform.position = position;

        AudioSource src = audioObj.AddComponent<AudioSource>();
        src.clip = explosionSfx;
        src.volume = volume;
        src.pitch = Random.Range(pitchRange.x, pitchRange.y);
        src.spatialBlend = 0f; // 3D sound
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 1f;
        src.maxDistance = radius * 2f;

        src.Play();
        Destroy(audioObj, src.clip.length / src.pitch);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
