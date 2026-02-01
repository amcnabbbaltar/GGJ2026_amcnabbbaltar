using UnityEngine;

[RequireComponent(typeof(Collider))]
public class OrbitingMask : MonoBehaviour
{
    public enum State { Orbiting, Thrown }
    private State state = State.Orbiting;

    public bool IsThrown => state == State.Thrown;

    [Header("Orbit")]
    public Transform center;
    public float radius = 1.5f;
    public float speed = 120f;
    public float heightOffset = 1f;
    public float baseAngle;

    [Header("Throw")]
    public float throwSpeed = 18f;
    public float throwLifetime = 5f;

    [Header("Throw Preview Feedback")]
    public bool previewPulseScale = true;
    public float previewScaleMultiplier = 1.15f;
    public float previewPulseSpeed = 8f;

    public bool previewEmission = false;              // optional
    public Color previewEmissionColor = Color.white;  // optional
    public float previewEmissionIntensity = 2f;

    static float ringSpin;

    Rigidbody rb;

    bool selectedForThrow;
    Vector3 baseScale;

    Renderer[] rends;
    MaterialPropertyBlock mpb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        baseScale = transform.localScale;

        rends = GetComponentsInChildren<Renderer>(true);
        mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (state == State.Orbiting && center != null)
        {
            ringSpin += speed * Time.deltaTime;

            float angleDeg = baseAngle + ringSpin;
            float angleRad = angleDeg * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
                Mathf.Cos(angleRad) * radius,
                heightOffset,
                Mathf.Sin(angleRad) * radius
            );

            transform.position = center.position + offset;
            transform.LookAt(center.position + Vector3.up * heightOffset);
        }

        // Preview feedback runs even while orbiting
        UpdatePreviewVisuals();
    }

    void UpdatePreviewVisuals()
    {
        if (!selectedForThrow)
        {
            if (previewPulseScale)
                transform.localScale = baseScale;

            if (previewEmission)
                SetEmission(false);

            return;
        }

        if (previewPulseScale)
        {
            float pulse = (Mathf.Sin(Time.time * previewPulseSpeed) * 0.5f + 0.5f); // 0..1
            float s = Mathf.Lerp(1f, previewScaleMultiplier, pulse);
            transform.localScale = baseScale * s;
        }

        if (previewEmission)
            SetEmission(true);
    }

    void SetEmission(bool enabled)
    {
        if (rends == null) return;

        Color c = previewEmissionColor * (enabled ? previewEmissionIntensity : 0f);

        for (int i = 0; i < rends.Length; i++)
        {
            var r = rends[i];
            if (!r) continue;

            r.GetPropertyBlock(mpb);
            mpb.SetColor("_EmissionColor", c);
            r.SetPropertyBlock(mpb);
        }
    }

    public void SetSelectedForThrow(bool selected)
    {
        selectedForThrow = selected;
    }

    public void ThrowFrom(Transform castPoint, Vector3 direction)
    {
        if (state == State.Thrown) return;

        // clear preview state on throw
        selectedForThrow = false;
        if (previewPulseScale) transform.localScale = baseScale;
        if (previewEmission) SetEmission(false);

        state = State.Thrown;

        if (castPoint != null)
        {
            transform.position = castPoint.position;
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        rb.isKinematic = false;

        direction = direction.normalized;
        rb.velocity = direction * throwSpeed;

        Destroy(gameObject, throwLifetime);
    }
}
