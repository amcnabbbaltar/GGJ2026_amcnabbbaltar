using UnityEngine;

public class HandHitbox : MonoBehaviour
{
    [Header("Hit Settings")]
    public LayerMask playerLayers;
    public float hitCooldown = 0.2f; // prevents multi-hits in same swing if they stay inside trigger
    public float damage = 10;

    [Header("Flash Override (optional)")]
    public bool overridePlayerFlashColor = false;
    public Color flashColor = Color.red; 

    bool active;
    float nextHitTime;
   
    void Awake()
    {
        // Start disabled until attack window
        SetActive(false);
    }

    public void SetActive(bool value)
    {
        active = value;
        // Optional: also disable collider component for cleanliness
        var col = GetComponent<Collider>();
        if (col) col.enabled = value;
    }

    void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    void OnTriggerStay(Collider other)
    {
        // Optional: if you want reliable hits even when entering isn’t called (fast anims), keep this.
        TryHit(other);
    }

    void TryHit(Collider other)
    {
        if (!active) return;
        if (Time.time < nextHitTime) return;

        if (((1 << other.gameObject.layer) & playerLayers) == 0)
            return;

        // Look for PlayerHitFlash on the player (or parent)
        var flash = other.GetComponent<PlayerHitFlash>();
        if (flash != null)
        {
            if (overridePlayerFlashColor)
                flash.flashColor = flashColor;

            flash.Flash();
        }

        nextHitTime = Time.time + hitCooldown;
    }
}
