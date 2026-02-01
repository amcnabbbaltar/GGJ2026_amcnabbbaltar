using System.Collections;
using UnityEngine;

public class HitReaction : MonoBehaviour
{
    [Header("Debug")]
    public bool debugCalls = true;
    public bool debugAnimator = true;
    public bool debugFlash = false;

    [Header("Animation")]
    public Animator animator;
    public string hitReactTrigger = "HitReact";
    public float minTimeBetweenReacts = 0.08f;

    float nextReactTime;

    [Header("Flash Settings")]
    public Color flashColor = Color.red;
    public float flashDuration = 0.12f;

    public Transform renderRoot;

    [Header("Camera Shake")]
    public TopDownThirdPersonCamera camera;
    public float shakeDuration = 0.15f;
    public float shakeMagnitude = 0.12f;

    Renderer[] renderers;
    MaterialPropertyBlock mpb;

    struct OrigColor
    {
        public int rendererIndex;
        public int materialIndex;
        public Color color;
    }

    OrigColor[] originalColors;
    int origCount;

    Coroutine flashRoutine;

    void Awake()
    {
        if (!animator)
            animator = GetComponentInChildren<Animator>();

        if (debugAnimator)
        {
            if (!animator)
                Debug.LogWarning($"[{name}] HitReaction: Animator NOT found");
            else
                Debug.Log($"[{name}] HitReaction: Animator = {animator.gameObject.name}");
        }

        if (renderRoot == null)
            renderRoot = transform;

        renderers = renderRoot.GetComponentsInChildren<Renderer>(true);
        mpb = new MaterialPropertyBlock();

        var temp = new System.Collections.Generic.List<OrigColor>(64);

        for (int r = 0; r < renderers.Length; r++)
        {
            var mats = renderers[r].sharedMaterials;
            for (int m = 0; m < mats.Length; m++)
            {
                if (mats[m] != null && mats[m].HasProperty("_Color"))
                {
                    temp.Add(new OrigColor
                    {
                        rendererIndex = r,
                        materialIndex = m,
                        color = mats[m].GetColor("_Color")
                    });
                }
            }
        }

        originalColors = temp.ToArray();
        origCount = originalColors.Length;
    }

    /// <summary>Call this when damage happens.</summary>
    public void React()
    {
        if (debugCalls)
            Debug.Log($"[{name}] HitReaction.React() called at {Time.time:F2}");

        // ───── Animation ─────
        if (!animator)
        {
            if (debugAnimator)
                Debug.LogWarning($"[{name}] HitReaction: Animator is NULL");
        }
        else if (string.IsNullOrEmpty(hitReactTrigger))
        {
            if (debugAnimator)
                Debug.LogWarning($"[{name}] HitReaction: Trigger name is empty");
        }
        else if (Time.time < nextReactTime)
        {
            if (debugAnimator)
                Debug.Log($"[{name}] HitReaction: React blocked by cooldown");
        }
        else
        {
            nextReactTime = Time.time + minTimeBetweenReacts;

            animator.ResetTrigger(hitReactTrigger);
            animator.SetTrigger(hitReactTrigger);

            if (debugAnimator)
                Debug.Log($"[{name}] HitReaction: Triggered '{hitReactTrigger}'");
        }

        // ───── Flash / Shake ─────
        Flash();
    }

    public void Flash()
    {
        if (debugFlash)
            Debug.Log($"[{name}] HitReaction: Flash()");

        if (camera != null)
            camera.Shake(shakeDuration, shakeMagnitude);

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashCo());
    }

    IEnumerator FlashCo()
    {
        if (debugFlash)
            Debug.Log($"[{name}] HitReaction: Flash START");

        ApplyColorToAll(flashColor);
        yield return new WaitForSeconds(flashDuration);
        RestoreOriginalColors();

        if (debugFlash)
            Debug.Log($"[{name}] HitReaction: Flash END");

        flashRoutine = null;
    }

    void ApplyColorToAll(Color c)
    {
        for (int r = 0; r < renderers.Length; r++)
        {
            var rend = renderers[r];
            if (!rend) continue;

            int matCount = rend.sharedMaterials.Length;
            for (int m = 0; m < matCount; m++)
            {
                rend.GetPropertyBlock(mpb, m);
                mpb.SetColor("_Color", c);
                rend.SetPropertyBlock(mpb, m);
            }
        }
    }

    void RestoreOriginalColors()
    {
        for (int r = 0; r < renderers.Length; r++)
        {
            var rend = renderers[r];
            if (!rend) continue;

            int matCount = rend.sharedMaterials.Length;
            for (int m = 0; m < matCount; m++)
                rend.SetPropertyBlock(null, m);
        }

        for (int i = 0; i < origCount; i++)
        {
            var o = originalColors[i];
            var rend = renderers[o.rendererIndex];
            if (!rend) continue;

            rend.GetPropertyBlock(mpb, o.materialIndex);
            mpb.SetColor("_Color", o.color);
            rend.SetPropertyBlock(mpb, o.materialIndex);
        }
    }
}
