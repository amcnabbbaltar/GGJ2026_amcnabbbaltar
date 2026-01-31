using UnityEngine;

public class CharacterController : MonoBehaviour
{
    [Header("Move")]
    public float walkSpeed = 4f;
    public float sprintSpeed = 8f;
    public float acceleration = 25f;
    public float rotateSpeed = 720f;

    [Header("Air Control")]
    [Tooltip("0 = no air control (pure momentum). Small values = slight steering in air.")]
    public float airAcceleration = 4f;
    public float airMaxSpeed = 8f;

    [Header("Jump (feel)")]
    public float jumpImpulse = 6f;

    [Tooltip("Allows a jump shortly after walking off an edge (seconds).")]
    public float coyoteTime = 0.12f;

    [Tooltip("If you pressed jump shortly before landing, jump triggers on landing (seconds).")]
    public float jumpBufferTime = 0.12f;

    [Tooltip("Extra gravity when falling for snappier landings (>=1).")]
    public float fallGravityMultiplier = 2.2f;

    [Tooltip("Extra gravity when rising but jump released early (>=1).")]
    public float lowJumpGravityMultiplier = 2.0f;

    [Tooltip("Max downward speed to avoid insane falls.")]
    public float maxFallSpeed = 25f;

    [Header("Ground Check")]
    public float groundCheckDistance = 0.2f;
    public LayerMask groundMask = ~0;

    [Header("Refs")]
    public Rigidbody rb;
    public Animator animator;

    // Jump timing
    float coyoteTimer;
    float jumpBufferTimer;
    bool jumpHeld;

    // Air momentum
    bool wasGrounded;
    Vector3 storedAirPlanarVel; // XZ velocity we had when leaving ground

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            jumpBufferTimer = jumpBufferTime;

        jumpHeld = Input.GetKey(KeyCode.Space);
    }

    void FixedUpdate()
    {
        bool grounded = IsGrounded();

        // Timers
        if (grounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.fixedDeltaTime;

        jumpBufferTimer -= Time.fixedDeltaTime;

        // Input
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(x, 0f, y).normalized;

        bool sprint = Input.GetKey(KeyCode.LeftShift);
        float maxSpeed = sprint ? sprintSpeed : walkSpeed;

        if (PlayerMovementLock.Instance != null && PlayerMovementLock.Instance.locked)
            input = Vector3.zero;

        // Detect leaving the ground (walk off OR jump)
        if (wasGrounded && !grounded)
        {
            Vector3 planar = rb.velocity;
            planar.y = 0f;
            storedAirPlanarVel = planar;
        }
        wasGrounded = grounded;

        // --- Movement ---
        if (grounded)
        {
            // Ground acceleration (your original)
            Vector3 targetVelocity = input * maxSpeed;

            Vector3 currentVelocity = rb.velocity;
            currentVelocity.y = 0f;

            Vector3 force = (targetVelocity - currentVelocity) * acceleration;
            rb.AddForce(force, ForceMode.Acceleration);
        }
        else
        {
            // Airborne: keep the floor momentum direction/speed
            Vector3 v = rb.velocity;
            v.x = storedAirPlanarVel.x;
            v.z = storedAirPlanarVel.z;
            rb.velocity = v;

            // Optional: small steering in air
            if (airAcceleration > 0f && input.sqrMagnitude > 0.0001f)
            {
                Vector3 desired = input * airMaxSpeed;
                Vector3 currentPlanar = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

                Vector3 airForce = (desired - currentPlanar) * airAcceleration;
                rb.AddForce(airForce, ForceMode.Acceleration);

                // Update stored momentum so it doesn't snap back next frame
                Vector3 newPlanar = rb.velocity;
                newPlanar.y = 0f;
                storedAirPlanarVel = newPlanar;
            }
        }

        // Rotate (optional: you can keep this even in air, feels fine)
        if (input.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(input, Vector3.up);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot, rotateSpeed * Time.fixedDeltaTime));
        }

        // --- Jump (buffer + coyote) ---
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;

            // Store planar velocity at takeoff so air keeps it
            Vector3 takeoffPlanar = rb.velocity;
            takeoffPlanar.y = 0f;
            storedAirPlanarVel = takeoffPlanar;

            // cancel downward velocity for consistent takeoff
            Vector3 v = rb.velocity;
            if (v.y < 0f) v.y = 0f;
            rb.velocity = v;

            rb.AddForce(Vector3.up * jumpImpulse, ForceMode.Impulse);
        }

        var dash = GetComponent<RigidbodyDash>();
        if (dash != null && dash.IsDashing)
        {
            ApplyBetterGravity();
            return;
        }


        // Animator
        if (animator)
        {
            float speedAnim = rb.velocity.magnitude / sprintSpeed;

            float direction = 0f;
            if (input.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(input.x, input.z);
                direction = angle / Mathf.PI;
            }

            animator.SetBool("IsGrounded", grounded);
            animator.SetFloat("Speed", speedAnim);
            animator.SetFloat("Direction", direction);

            if (Input.GetButtonUp("Fire1"))
                animator.SetTrigger("Spawn");
            if (Input.GetButtonUp("Fire2"))
                animator.SetTrigger("Attack");
        }
    }

    void ApplyBetterGravity()
    {
        Vector3 v = rb.velocity;

        if (v.y < 0f)
        {
            rb.AddForce(Physics.gravity * (fallGravityMultiplier - 1f), ForceMode.Acceleration);
            v.y = Mathf.Max(v.y, -maxFallSpeed);
            rb.velocity = v;
        }
        else if (v.y > 0f && !jumpHeld)
        {
            rb.AddForce(Physics.gravity * (lowJumpGravityMultiplier - 1f), ForceMode.Acceleration);
        }
    }

    public bool IsGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        return Physics.Raycast(origin, Vector3.down, 0.1f + groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
    }
}
