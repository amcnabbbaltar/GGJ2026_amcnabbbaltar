using System.Collections.Generic;
using UnityEngine;

public class MaskSpellCaster : MonoBehaviour
{
    [Header("References")]
    public GameObject maskPrefab;
    public Transform orbitCenter;
    public Camera aimCamera;

    [Header("Aim Line (Fancy)")]
    public LineRenderer aimLine;
    public float lineStartHeight = 1.0f;
    public float lineEndHeightOffset = 0.02f;
    public LayerMask aimLayerMask = ~0;
    public float maxAimDistance = 50f;
    public bool showLineWhenNoMasks = false;
    public Transform castPoint;

    [Header("Optional: Ground Reticle")]
    public GameObject aimIndicatorPrefab;
    private GameObject aimIndicatorInstance;

    [Header("Orbit Levels")]
    public float baseRadius = 1.2f;
    public float radiusStep = 0.5f;
    public float baseHeight = 1.0f;
    public float heightStep = 0.35f;
    public float orbitSpeed = 120f;
    public int masksPerLevel = 3;

    [Header("Throw")]
    public bool throwAllMasks = true;

    [Header("Limit")]
    public int maxMasks = 9;
    public bool replaceOldestWhenFull = false;

    [Header("Throw Timing")]
    public float throwDelay = 1f;
    public bool blockWhileThrowing = true;

    [Header("Throw Rotation")]
    [Tooltip("What rotates to face the throw direction. If null, uses this transform.")]
    public Transform rotateRoot;

    [Tooltip("If true, snap instantly to the throw direction when you press Fire2.")]
    public bool snapTurnOnThrow = true;

    [Tooltip("If false, smoothly rotate over time.")]
    public float turnSpeed = 1080f; // degrees/sec

    [Tooltip("If true, keep rotating during the windup.")]
    public bool rotateDuringWindup = true;

    [Tooltip("If true, aim direction is locked at button press. If false, updates until release.")]
    public bool lockAimDuringThrow = true;

    [Header("Shotgun Throw")]
    public bool shotgunCone = true;
    public float coneAngle = 30f;          // total cone angle in degrees
    public bool randomSpread = false;       // true = random shotgun, false = evenly spaced
    public float throwAllInterval = 0.02f;  // optional delay between each mask (0 = instant)


    bool isThrowing;
    Coroutine throwRoutine;
    Vector3 queuedDirection;

    

    private readonly List<OrbitingMask> masks = new();
    private int spawnIndex;

    public Animator animator;

    void Awake()
    {
        if (orbitCenter == null) orbitCenter = transform;
        if (aimCamera == null) aimCamera = Camera.main;
        if (rotateRoot == null) rotateRoot = transform;

        if (aimIndicatorPrefab != null)
            aimIndicatorInstance = Instantiate(aimIndicatorPrefab);

        if (aimLine != null)
        {
            aimLine.positionCount = 2;
            aimLine.enabled = false;
        }
    }

    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
            SpawnMask();

        // Update aim UI every frame
        UpdateAimUI(out Vector3 aimDir);

        if (Input.GetButtonDown("Fire2"))
        {
            queuedDirection = aimDir;

            // Turn immediately so the animation starts facing the right way
            TurnTowards(queuedDirection, snapTurnOnThrow);

            RequestThrow();
        }

        RecalculateAnglesPerLevel();
    }

    void UpdateAimUI(out Vector3 aimDirection)
    {
        bool hasMasks = masks.Count > 0;
        bool shouldShow = showLineWhenNoMasks || hasMasks;

        Vector3 start = orbitCenter.position + Vector3.up * lineStartHeight;

        aimDirection = transform.forward;

        if (aimCamera == null)
        {
            SetLine(shouldShow, start, start + aimDirection * maxAimDistance);
            SetReticle(false, Vector3.zero);
            return;
        }

        Ray ray = aimCamera.ScreenPointToRay(Input.mousePosition);

        Vector3 end;
        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimLayerMask))
        {
            end = hit.point + Vector3.up * lineEndHeightOffset;

            Vector3 dir = (hit.point - orbitCenter.position);
            dir.y = 0f;
            aimDirection = dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;

            SetReticle(true, end);
        }
        else
        {
            Vector3 forward = aimCamera.transform.forward;
            forward.y = 0f;
            aimDirection = forward.sqrMagnitude > 0.0001f ? forward.normalized : transform.forward;

            end = start + aimDirection * maxAimDistance;
            SetReticle(false, Vector3.zero);
        }

        SetLine(shouldShow, start, end);
    }

    void SetLine(bool enabled, Vector3 start, Vector3 end)
    {
        if (aimLine == null) return;

        aimLine.enabled = enabled;
        if (!enabled) return;

        aimLine.SetPosition(0, start);
        aimLine.SetPosition(1, end);
    }

    void SetReticle(bool enabled, Vector3 position)
    {
        if (aimIndicatorInstance == null) return;

        aimIndicatorInstance.SetActive(enabled);
        if (enabled) aimIndicatorInstance.transform.position = position;
    }

    void SpawnMask()
    {
        if (maskPrefab == null) return;
        if (masks.Count >= maxMasks) return;

        var obj = Instantiate(maskPrefab);
        var mask = obj.GetComponent<OrbitingMask>() ?? obj.AddComponent<OrbitingMask>();

        int level = spawnIndex / Mathf.Max(1, masksPerLevel);

        mask.center = orbitCenter;
        mask.radius = baseRadius + radiusStep * level;
        mask.heightOffset = baseHeight + heightStep * level;
        mask.speed = orbitSpeed;

        masks.Add(mask);
        spawnIndex++;
    }

    void RequestThrow()
    {
        if (blockWhileThrowing && isThrowing) return;

        if (throwRoutine != null) StopCoroutine(throwRoutine);
        throwRoutine = StartCoroutine(ThrowAfterDelay());
    }

    System.Collections.IEnumerator ThrowAfterDelay()
    {
        isThrowing = true;

        if (animator) animator.SetTrigger("Throw");

        float t = 0f;
        while (t < throwDelay)
        {
            t += Time.deltaTime;

            // Optionally keep updating aim during windup
            if (!lockAimDuringThrow)
            {
                UpdateAimUI(out var aimDirNow);
                queuedDirection = aimDirNow;
            }

            // Optionally keep rotating during windup
            if (rotateDuringWindup && !snapTurnOnThrow)
                TurnTowards(queuedDirection, snap: false);

            yield return null;
        }

        ThrowMasks(queuedDirection);

        isThrowing = false;
        throwRoutine = null;
    }

    void ThrowMasks(Vector3 direction)
    {
        masks.RemoveAll(m => m == null);
        if (masks.Count == 0) return;

        if (!throwAllMasks)
        {
            ThrowOne(direction);
            return;
        }

        if (shotgunCone)
        {
            StartCoroutine(ThrowAllShotgun(direction));
        }
        else
        {
            // Normal "all same direction"
            StartCoroutine(ThrowAllSameDirection(direction));
        }
    }

    void ThrowOne(Vector3 direction)
    {
        Transform from = castPoint != null ? castPoint : orbitCenter;

        OrbitingMask mask = masks[masks.Count - 1];
        masks.RemoveAt(masks.Count - 1);

        if (mask != null)
            mask.ThrowFrom(from, Flatten(direction));
    }

    System.Collections.IEnumerator ThrowAllSameDirection(Vector3 direction)
    {
        Transform from = castPoint != null ? castPoint : orbitCenter;

        var toThrow = new List<OrbitingMask>(masks);
        masks.Clear();

        Vector3 dir = Flatten(direction);

        for (int i = 0; i < toThrow.Count; i++)
        {
            var mask = toThrow[i];
            if (mask != null)
                mask.ThrowFrom(from, dir);

            if (throwAllInterval > 0f)
                yield return new WaitForSeconds(throwAllInterval);
        }
    }

    System.Collections.IEnumerator ThrowAllShotgun(Vector3 direction)
    {
        Transform from = castPoint != null ? castPoint : orbitCenter;

        var toThrow = new List<OrbitingMask>(masks);
        masks.Clear();

        Vector3 baseDir = Flatten(direction);
        int n = toThrow.Count;

        if (n == 1)
        {
            if (toThrow[0] != null) toThrow[0].ThrowFrom(from, baseDir);
            yield break;
        }

        // Spread from -half to +half degrees
        float half = coneAngle * 0.5f;

        for (int i = 0; i < n; i++)
        {
            float angle;

            if (randomSpread)
            {
                angle = Random.Range(-half, half);
            }
            else
            {
                // Evenly spaced across the cone
                float t = (n == 1) ? 0.5f : (float)i / (n - 1);
                angle = Mathf.Lerp(-half, half, t);
            }

            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * baseDir;

            var mask = toThrow[i];
            if (mask != null)
                mask.ThrowFrom(from, dir);

            if (throwAllInterval > 0f)
                yield return new WaitForSeconds(throwAllInterval);
        }
    }

    Vector3 Flatten(Vector3 v)
    {
        v.y = 0f;
        if (v.sqrMagnitude < 0.0001f) return transform.forward;
        return v.normalized;
    }



    void TurnTowards(Vector3 direction, bool snap)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(direction.normalized, Vector3.up);

        if (snap)
        {
            rotateRoot.rotation = targetRot;
        }
        else
        {
            float step = turnSpeed * Time.deltaTime;
            rotateRoot.rotation = Quaternion.RotateTowards(rotateRoot.rotation, targetRot, step);
        }
    }

    void RecalculateAnglesPerLevel()
    {
        var levelMap = new Dictionary<int, List<OrbitingMask>>();

        foreach (var mask in masks)
        {
            if (mask == null) continue;
            int level = Mathf.RoundToInt((mask.radius - baseRadius) / Mathf.Max(0.0001f, radiusStep));
            if (!levelMap.ContainsKey(level)) levelMap[level] = new List<OrbitingMask>();
            levelMap[level].Add(mask);
        }

        foreach (var kvp in levelMap)
        {
            float step = 360f / kvp.Value.Count;
            for (int i = 0; i < kvp.Value.Count; i++)
                kvp.Value[i].baseAngle = step * i;
        }
    }
}
