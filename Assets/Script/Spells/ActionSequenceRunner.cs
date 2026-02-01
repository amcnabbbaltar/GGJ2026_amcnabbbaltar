using System;
using System.Collections;
using UnityEngine;

public class ActionSequenceRunner : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;

    [Header("VFX")]
    public GameObject vfxPrefab;
    public Transform vfxSpawnPoint;
    public Vector3 vfxPositionOffset;
    public Vector3 vfxRotationOffset; // euler
    public bool attachVfxToSpawnPoint = true;
    public float vfxDestroyAfter = 2f;

    [Header("Timing (seconds)")]
    public float vfxDelaySeconds = 0f;

    Coroutine routine;
    GameObject activeVfxObj;
    ParticleSystem activePS;

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!vfxSpawnPoint) vfxSpawnPoint = transform;
    }

    public bool IsRunning => routine != null;

    public void StopAll()
    {
        CancelInvoke();
        if (routine != null) StopCoroutine(routine);
        routine = null;
        StopVfx();
    }

    /// <summary>
    /// Simple sequence:
    /// - Trigger anim now
    /// - Spawn VFX after vfxDelaySeconds
    /// - Call onA at timeA
    /// - Call onB at timeB
    /// - End after totalDuration, then onEnd
    /// </summary>
    public void Play(
        string triggerName,
        float totalDuration,
        Action onStart = null,
        float timeA = -1f, Action onA = null,
        float timeB = -1f, Action onB = null,
        Action onEnd = null,
        float vfxDelayOverrideSeconds = -1f)
    {
        StopAll();

        onStart?.Invoke();

        if (animator && !string.IsNullOrEmpty(triggerName))
            animator.SetTrigger(triggerName);

        float vfxDelay = (vfxDelayOverrideSeconds >= 0f) ? vfxDelayOverrideSeconds : vfxDelaySeconds;

        // schedule vfx + callbacks (Invoke is simple + cheap)
        if (vfxDelay > 0f) Invoke(nameof(SpawnVfx), vfxDelay);
        else SpawnVfx();

        if (timeA >= 0f && onA != null) InvokeAction(onA, timeA);
        if (timeB >= 0f && onB != null) InvokeAction(onB, timeB);

        routine = StartCoroutine(EndAfter(totalDuration, onEnd));
    }

    // ---------- invoke helper ----------
    void InvokeAction(Action a, float delay)
    {
        // Use coroutine so we can pass a delegate (Invoke can't pass params)
        StartCoroutine(InvokeActionCo(a, delay));
    }

    IEnumerator InvokeActionCo(Action a, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        a?.Invoke();
    }

    IEnumerator EndAfter(float duration, Action onEnd)
    {
        if (duration > 0f)
            yield return new WaitForSeconds(duration);

        onEnd?.Invoke();
        StopVfx();
        routine = null;
    }

    // ---------- VFX ----------
    void SpawnVfx()
    {
        if (!vfxPrefab) return;

        // cleanup previous
        if (activeVfxObj != null)
        {
            Destroy(activeVfxObj);
            activeVfxObj = null;
            activePS = null;
        }

        Transform sp = vfxSpawnPoint ? vfxSpawnPoint : transform;

        Vector3 worldPos = sp.position + sp.TransformDirection(vfxPositionOffset);
        Quaternion worldRot = sp.rotation * Quaternion.Euler(vfxRotationOffset);

        activeVfxObj = Instantiate(vfxPrefab, worldPos, worldRot);

        if (attachVfxToSpawnPoint)
            activeVfxObj.transform.SetParent(sp, true);

        activePS = activeVfxObj.GetComponent<ParticleSystem>() ?? activeVfxObj.GetComponentInChildren<ParticleSystem>();
        if (activePS) activePS.Play(true);

        if (!attachVfxToSpawnPoint)
            Destroy(activeVfxObj, vfxDestroyAfter);
    }

    void StopVfx()
    {
        if (activeVfxObj == null) return;

        if (activePS != null)
            activePS.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        Destroy(activeVfxObj, vfxDestroyAfter);
        activeVfxObj = null;
        activePS = null;
    }
}
