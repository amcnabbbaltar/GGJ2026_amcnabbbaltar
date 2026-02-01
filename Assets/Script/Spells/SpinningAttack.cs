using UnityEngine;

public class PlayerMeleeAttack : MonoBehaviour
{
    [Header("Refs")]
    public ActionSequenceRunner sequence;
    public Hitbox weaponHitbox;

    [Header("Input")]
    public string fireButton = "Fire2";

    [Header("Cooldown")]
    public float cooldown = 1.2f;
    public bool cooldownStartsOnEnd = true;

    [Header("Timing")]
    public float totalDuration = 0.9f;        // whole attack
    public float hitboxOnDelay = 0.15f;       // after start
    public float hitboxActiveDuration = 0.5f; // damage window

    [Header("Animator")]
    public string attackTriggerName = "Attack";

    [Header("VFX")]
    [Tooltip("Override frame delay for this attack (-1 = use runner default).")]
    public int fxDelayFrames = -1;

    bool attacking;
    float nextAllowedTime;

    void Awake()
    {
        if (!sequence)
            sequence = GetComponent<ActionSequenceRunner>();

        if (weaponHitbox)
            weaponHitbox.SetActive(false);
    }

    void Update()
    {
        if (Input.GetButtonDown(fireButton))
            TryAttack();
    }

    void TryAttack()
    {
        if (Time.time < nextAllowedTime) return;
        if (attacking) return;
        if (!sequence || !weaponHitbox) return;

        if (!cooldownStartsOnEnd)
            nextAllowedTime = Time.time + cooldown;

        attacking = true;
        weaponHitbox.SetActive(false);

        sequence.Play(
            triggerName: "Attack",
            totalDuration: totalDuration,
            timeA: hitboxOnDelay, onA: () => weaponHitbox.SetActive(true),
            timeB: hitboxOnDelay + hitboxActiveDuration, onB: () => weaponHitbox.SetActive(false),
            onEnd: () => weaponHitbox.SetActive(false)
        );
    }

    void OnDisable()
    {
        attacking = false;

        if (weaponHitbox)
            weaponHitbox.SetActive(false);

        if (sequence)
            sequence.StopAll();
    }
}
