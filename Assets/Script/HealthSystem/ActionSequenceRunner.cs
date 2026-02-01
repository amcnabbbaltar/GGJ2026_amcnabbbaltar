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

    [Header("SFX")]
    public AudioSource audioSource;                 // used for PlayOneShot
    public AudioClip startSfx;                      // plays immediately on Play()
    public AudioClip vfxSfx;                        // plays when VFX spawns
    public float vfxSfxDelaySeconds = 0f;           // additional delay relative to VFX spawn time
    [Range(0f, 1f)] public float sfxVolume = 1f;

    Coroutine routine;
    GameObject activeVfxObj;
    ParticleSystem activePS;

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!vfxSpawnPoint) vfxSpawnPoint = transform;

        if (!audioSource)
        {
            audioSource = GetComponent<AudioSource>();
            if (!audioSource) audioSource = GetComponentInChildren<AudioSource>();
        }
    }

    public bool IsRunning => routine != null;

    public void StopAll()
    {
        CancelInvoke();
        if (routine != null) StopCoroutine(routine);
        routine = null;

        StopVfx();
        // We intentionally do NOT stop audioSource, because PlayOneShot clips are typically allowed to finish.
        // If you want to stop everything, uncomment the line below:
        // if (audioSource) audioSource.Stop();
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
        float vfxDelayOverrideSeconds = -1f,

        // ---- SFX optional overrides ----
        AudioClip startSfxOverride = null,
        AudioClip vfxSfxOverride = null,
        float vfxSfxDelayOverrideSeconds = -1f)
    {
        StopAll();

        onStart?.Invoke();

        // start sfx
        PlaySfx(startSfxOverride ? startSfxOverride : startSfx);

        if (animator && !string.IsNullOrEmpty(triggerName))
            animator.SetTrigger(triggerName);

        float vfxDelay = (vfxDelayOverrideSeconds >= 0f) ? vfxDelayOverrideSeconds : vfxDelaySeconds;

        // schedule vfx
        if (vfxDelay > 0f) Invoke(nameof(SpawnVfx), vfxDelay);
        else SpawnVfx();

        // schedule callbacks
        if (timeA >= 0f && onA != null) InvokeAction(onA, timeA);
        if (timeB >= 0f && onB != null) InvokeAction(onB, timeB);

        // If you want SFX at timeA/timeB too, you can do it by passing lambdas:
        // Play(..., timeA: 0.2f, onA: () => PlaySfx(myClip), ...)

        routine = StartCoroutine(EndAfter(totalDuration, onEnd, vfxDelay, vfxSfxOverride, vfxSfxDelayOverrideSeconds));
    }

    // ---------- invoke helper ----------
    void InvokeAction(Action a, float delay)
    {
        StartCoroutine(InvokeActionCo(a, delay));
    }

    IEnumerator InvokeActionCo(Action a, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        a?.Invoke();
    }

    IEnumerator EndAfter(float duration, Action onEnd, float vfxDelay, AudioClip vfxSfxOverride, float vfxSfxDelayOverrideSeconds)
    {
        // schedule VFX SFX relative to sequence start: (vfxDelay + optional extra)
        AudioClip clip = vfxSfxOverride ? vfxSfxOverride : vfxSfx;
        float extra = (vfxSfxDelayOverrideSeconds >= 0f) ? vfxSfxDelayOverrideSeconds : vfxSfxDelaySeconds;
        float sfxAt = Mathf.Max(0f, vfxDelay + extra);

        if (clip != null)
            StartCoroutine(PlaySfxAfter(clip, sfxAt));

        if (duration > 0f)
            yield return new WaitForSeconds(duration);

        onEnd?.Invoke();
        StopVfx();
        routine = null;
    }

    // ---------- SFX ----------
    void PlaySfx(AudioClip clip)
    {
        if (!clip || !audioSource) return;
        audioSource.PlayOneShot(clip, sfxVolume);
    }

    IEnumerator PlaySfxAfter(AudioClip clip, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        PlaySfx(clip);
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
