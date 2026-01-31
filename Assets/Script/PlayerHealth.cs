using System.Collections;
using UnityEngine;

public class PlayerHitFlash : MonoBehaviour
{
    [Header("Flash Settings")]
    public Color flashColor = Color.red;
    public float flashDuration = 0.12f;

    [Tooltip("If set, only renderers under this object will flash. Leave null to flash all in children.")]
    public Transform renderRoot;


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
        if (renderRoot == null) renderRoot = transform;


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

    public void Flash()
    {
        if (camera != null)
            camera.Shake(shakeDuration, shakeMagnitude);

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashCo());
    }


    IEnumerator FlashCo()
    {
        ApplyColorToAll(flashColor);
        yield return new WaitForSeconds(flashDuration);

        RestoreOriginalColors();
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
