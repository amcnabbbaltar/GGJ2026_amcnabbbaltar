using UnityEngine;

[RequireComponent(typeof(Collider))]
public class OrbitingMask : MonoBehaviour
{
    public enum State { Orbiting, Thrown }

    [Header("Orbit")]
    public Transform center;
    public float radius = 1.5f;
    public float speed = 120f;
    public float heightOffset = 1f;

    [Tooltip("Fixed slot angle in DEGREES. Keep unique per mask.")]
    public float baseAngle;

    [Header("Throw")]
    public float throwSpeed = 18f;
    public float throwLifetime = 5f;

    // Shared ring rotation (all masks stay aligned)
    static float ringSpin;

    private State state = State.Orbiting;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        if (state != State.Orbiting || center == null) return;

        // One shared spin for all masks -> they keep spacing and never "catch up" to each other
        ringSpin += speed * Time.deltaTime;

        float angleDeg = baseAngle + ringSpin;
        float angleRad = angleDeg * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Cos(angleRad) * radius,
            heightOffset,
            Mathf.Sin(angleRad) * radius
        );

        transform.position = center.position + offset;

        // Optional: face outward instead of looking at center
        // transform.rotation = Quaternion.LookRotation(offset.normalized, Vector3.up);

        // Your original look-at (faces center)
        transform.LookAt(center.position + Vector3.up * heightOffset);
    }

    public void ThrowFrom(Transform castPoint, Vector3 direction)
    {
        if (state == State.Thrown) return;

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
