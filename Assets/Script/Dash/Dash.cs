using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Dash : MonoBehaviour
{
    [Header("Dash")]
    public KeyCode dashKey = KeyCode.LeftControl;
    public float dashSpeed = 16f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.6f;

    [Header("Mouse Aim")]
    public LayerMask mouseAimMask = ~0;
    public bool allowAirDash = true;

    [Header("Visuals")]
    public GameObject dashStartFX;
    public GameObject dashEndFX;
    public float fxDestroyTime = 2f;

    [Tooltip("Root object to hide (usually mesh or model root).")]
    public GameObject visualRoot;

    [Header("Rotation")]
    public bool rotateTowardDash = true;

    [Header("Animator")]
    public Animator animator;                     // assign or auto-find
    public string isDashingBool = "IsDashing";    // Animator bool parameter name
    public bool writeAnimatorBool = true;         // toggle if you want to disable animator writes

    [Header("Refs")]
    public Rigidbody rb;
    public Camera cam;

    public bool IsDashing { get; private set; }

    float dashTimer;
    float cooldownTimer;
    Vector3 dashDir;

    CharacterController controller;
    public DashAfterImage afterImage;

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!cam) cam = Camera.main;

        if (!visualRoot)
            visualRoot = GetComponentInChildren<Renderer>()?.gameObject;

        controller = GetComponent<CharacterController>();

        if (!afterImage) afterImage = GetComponent<DashAfterImage>();

        if (!animator)
            animator = GetComponentInChildren<Animator>();

        // Ensure animator starts in correct state
        SetAnimDashing(false);
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(dashKey) && cooldownTimer <= 0f)
        {
            if (!allowAirDash && controller != null)
                return;

            StartDash();
        }
    }

    void FixedUpdate()
    {
        if (!IsDashing) return;

        dashTimer -= Time.fixedDeltaTime;

        float y = rb.velocity.y;

        rb.velocity = new Vector3(
            dashDir.x * dashSpeed,
            allowAirDash ? y : 0f,
            dashDir.z * dashSpeed
        );

        if (dashTimer <= 0f)
            EndDash();
    }

    void StartDash()
    {
        IsDashing = true;
        SetAnimDashing(true);

        dashTimer = dashDuration;
        cooldownTimer = dashCooldown;

        dashDir = GetMouseDashDirection();

        // Rotate character toward dash direction
        if (rotateTowardDash && dashDir.sqrMagnitude > 0.001f)
        {
            Quaternion rot = Quaternion.LookRotation(dashDir, Vector3.up);
            rb.MoveRotation(rot);
        }

        // FX at start
        SpawnFX(dashStartFX, transform.position);
        if (afterImage) afterImage.StartEmitting();

        // Hide character
        if (visualRoot)
            visualRoot.SetActive(false);

        // Clean start
        Vector3 v = rb.velocity;
        v.y = allowAirDash ? v.y : 0f;
        rb.velocity = v;
    }

    void EndDash()
    {
        IsDashing = false;
        SetAnimDashing(false);

        // Reappear FX
        SpawnFX(dashEndFX, transform.position);

        // Show character
        if (visualRoot)
            visualRoot.SetActive(true);

        if (afterImage) afterImage.StopEmitting();
    }

    void SetAnimDashing(bool value)
    {
        if (!writeAnimatorBool) return;
        if (!animator) return;
        if (string.IsNullOrEmpty(isDashingBool)) return;

        animator.SetBool(isDashingBool, value);
    }

    void SpawnFX(GameObject fxPrefab, Vector3 pos)
    {
        if (!fxPrefab) return;

        GameObject fx = Instantiate(fxPrefab, pos, Quaternion.identity);
        Destroy(fx, fxDestroyTime);
    }

    Vector3 GetMouseDashDirection()
    {
        if (!cam) return transform.forward;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, mouseAimMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 dir = hit.point - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.001f)
                return dir.normalized;
        }

        return transform.forward;
    }
}
