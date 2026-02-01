using System.Collections;
using UnityEngine;

public class MaskSpellCaster : MonoBehaviour
{
    [Header("Refs")]
    public MaskInventory inventory;
    public MaskAimUI aimUI;
    public MaskThrower thrower;

    [Header("Health Hook")]
    public Health health;
    public bool loseMaskOnHit = true;
    public int masksLostPerHit = 1;

    [Header("Auto Spawn")]
    public int startingMasks = 3;
    public bool autoRefill = false;
    public float refillInterval = 2.0f;
    public bool refillOnlyWhenNotFull = true;

    [Header("Charge Throw (Fire1 Hold)")]
    public float maxHoldTimeToFull = 1.25f; // hold this long to throw all masks
    public int minMasksOnTap = 1;           // quick tap throws at least this many

    System.Action<float> damageHandler;
    Coroutine refillRoutine;

    bool holding;
    float holdStartTime;
    Vector3 heldAimDir;

    void Awake()
    {
        if (!inventory) inventory = GetComponentInChildren<MaskInventory>();
        if (!aimUI) aimUI = GetComponentInChildren<MaskAimUI>();
        if (!thrower) thrower = GetComponentInChildren<MaskThrower>();

        if (!health) health = GetComponentInParent<Health>();

        if (thrower && inventory && thrower.inventory == null)
            thrower.inventory = inventory;
    }

    void OnEnable()
    {
        if (health != null && loseMaskOnHit)
        {
            damageHandler = _ => inventory?.Lose(masksLostPerHit);
            health.OnDamaged += damageHandler;
        }
    }

    void OnDisable()
    {
        if (health != null && damageHandler != null)
            health.OnDamaged -= damageHandler;

        damageHandler = null;
    }

    void Start()
    {
        if (inventory) inventory.SpawnMany(startingMasks);

        if (autoRefill)
            refillRoutine = StartCoroutine(RefillLoop());
    }

    IEnumerator RefillLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(refillInterval);

            if (!inventory) continue;

            if (refillOnlyWhenNotFull && inventory.Count >= inventory.maxMasks)
                continue;

            inventory.SpawnOne();
        }
    }

    void Update()
    {
        if (!inventory || !aimUI || !thrower) return;

        // always update aim visuals
        Vector3 aimDirNow = aimUI.UpdateAim(inventory.HasMasks);

        // START HOLD
        if (Input.GetButtonDown("Fire1"))
        {
            if (!inventory.HasMasks) return;
            if (!thrower.CanStartThrow()) return;

            holding = true;
            holdStartTime = Time.time;
            heldAimDir = aimDirNow;
        }

        // WHILE HOLDING: keep aim fresh
        if (holding)
        {
            heldAimDir = aimDirNow; 
            int charged = ComputeChargedCount();
            inventory.PreviewThrowCount(charged);
        }

        // RELEASE -> throw charged amount
        if (Input.GetButtonUp("Fire1") && holding)
        {
            

            if (!inventory.HasMasks) return;
            holding = false;
            int amountToThrow = ComputeChargedCount();
            inventory.ClearPreview();
            if (amountToThrow <= 0) return;

            // live aim provider for windup (only used if thrower.lockAimDuringThrow == false)
            System.Func<Vector3> liveAim = () => aimUI.UpdateAim(inventory.HasMasks);

            thrower.RequestThrow(heldAimDir, amountToThrow, liveAim);
        }
    }

    int ComputeChargedCount()
    {
        int total = inventory.Count;
        if (total <= 0) return 0;

        float held = Mathf.Max(0f, Time.time - holdStartTime);
        float t = (maxHoldTimeToFull <= 0.01f) ? 1f : Mathf.Clamp01(held / maxHoldTimeToFull);

        int count = Mathf.RoundToInt(Mathf.Lerp(minMasksOnTap, total, t));
        return Mathf.Clamp(count, 1, total);
    }
}
