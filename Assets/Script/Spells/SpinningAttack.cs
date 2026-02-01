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
    MaskAimUI aimUI;

    void Awake()
    {
        if (!sequence)
            sequence = GetComponent<ActionSequenceRunner>();

        if (weaponHitbox)
            weaponHitbox.SetActive(false);
        
        aimUI = FindObjectOfType<MaskAimUI>();
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

        Vector3 aimDirection = aimUI.UpdateAim(true);
        TurnTowards(aimDirection, true);
        attacking = true;
        weaponHitbox.SetActive(false);

        // cooldown starts immediately
        if (!cooldownStartsOnEnd)
            nextAllowedTime = Time.time + cooldown;

        sequence.Play(
            triggerName: attackTriggerName, // use your variable instead of hardcoding
            totalDuration: totalDuration,
            timeA: hitboxOnDelay, onA: () => weaponHitbox.SetActive(true),
            timeB: hitboxOnDelay + hitboxActiveDuration, onB: () => weaponHitbox.SetActive(false),
            onEnd: () =>
            {
                weaponHitbox.SetActive(false);
                attacking = false;

                // cooldown starts when attack ends
                if (cooldownStartsOnEnd)
                    nextAllowedTime = Time.time + cooldown;
            }
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
    void TurnTowards(Vector3 direction, bool snap)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(direction.normalized, Vector3.up);
        if (snap)
        {
            transform.rotation = targetRot;
        }
        else
        {
            float turnSpeed = 720f; // degrees per second
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                turnSpeed * Time.deltaTime
            );
        }
    }
}
