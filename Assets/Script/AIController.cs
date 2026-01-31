using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class AIController : MonoBehaviour
{
    [Header("Refs")]
    public NavMeshAgent agent;
    public Animator animator;
    public Transform player;

    [Header("Ranges")]
    public float aggroRange = 10f;
    public float attackRange = 1.6f;

    [Header("Attack")]
    public float attackCooldown = 1.2f;
    public float attackLockTime = 0.7f; // fallback if no anim event
    public float attackHitboxActivationDelay = 0.5f;
    [Header("Flocking")]
    public float neighborRadius = 6f;
    public float separationRadius = 1.5f;
    public float cohesionWeight = 1.0f;
    public float separationWeight = 1.5f;
    public float wanderWeight = 0.2f; // used to add drift to roaming target

    [Header("Timing")]
    public float updatePathInterval = 0.2f;

    [Header("Idle")]
    public float idleChancePerTick = 0.02f;
    public float idleDuration = 1.0f;

    [Header("Wander")]
    public float wanderRadius = 8f;
    public float wanderMinWait = 1.0f;
    public float wanderMaxWait = 3.0f;

    static readonly List<AIController> flock = new List<AIController>();

    float nextUpdate;
    float nextAttackTime;
    bool idling;
    float idleUntil;

    bool isAttacking;
    float attackUnlockTime;

    // NEW wandering state
    float nextWanderPickTime;
    Vector3 currentWanderTarget;
    bool hasWanderTarget;

    void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
    }

    void OnEnable() => flock.Add(this);
    void OnDisable() => flock.Remove(this);

    void Start()
    {
        if (agent)
        {
            agent.updateRotation = true;
            agent.angularSpeed = 360f;
        }

        nextUpdate = Time.time + Random.Range(0f, updatePathInterval);
        nextWanderPickTime = Time.time + Random.Range(wanderMinWait, wanderMaxWait);
    }

    void Update()
    {
        if (!agent || !animator || !player) return;

        // If attacking, do not navigate anywhere
        if (isAttacking)
        {
            agent.isStopped = true;
            agent.ResetPath();

            if (Time.time >= attackUnlockTime)
                EndAttackLock();

            FacePlayer();

            animator.SetFloat("Speed", 0f, 0.05f, Time.deltaTime);
            animator.SetFloat("Direction", 0f, 0.05f, Time.deltaTime);
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);

        // ATTACK if nearby
        if (dist <= attackRange)
        {
            DoAttack();
            return;
        }

        // CHASE if within aggro
        if (dist <= aggroRange)
        {
            idling = false;
            hasWanderTarget = false; // NEW: cancel wandering when player is close
            TickSetDestination(player.position);
            UpdateAnimatorFromAgent();
            return;
        }

        // IDLE handling (only when not aggro)
        if (idling)
        {
            if (Time.time >= idleUntil) idling = false;
            else
            {
                agent.ResetPath();
                animator.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);
                animator.SetFloat("Direction", 0f, 0.1f, Time.deltaTime);
                return;
            }
        }

        // Non-aggro behaviors (wander + small flock drift)
        if (Time.time >= nextUpdate)
        {
            nextUpdate = Time.time + updatePathInterval;

            // Random idle
            if (Random.value < idleChancePerTick)
            {
                idling = true;
                idleUntil = Time.time + idleDuration;
                animator.SetTrigger("DoIdle");
                agent.ResetPath();
                return;
            }

            // NEW: pick a new roam destination sometimes, or when reached
            bool reached =
                !agent.pathPending &&
                agent.hasPath &&
                agent.remainingDistance <= Mathf.Max(agent.stoppingDistance + 0.2f, 0.4f);

            if (!hasWanderTarget || reached || Time.time >= nextWanderPickTime)
            {
                PickNewWanderTarget();
            }

            // NEW: blend flock drift into the wander target so groups look alive
            Vector3 target = currentWanderTarget;

            Vector3 flockDir = ComputeFlockDirection();
            if (flockDir.sqrMagnitude > 0.001f)
                target += flockDir * (wanderWeight * 3f);

            if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }

        UpdateAnimatorFromAgent();
    }

    void DoAttack()
    {
        agent.isStopped = true;
        agent.ResetPath();

        FacePlayer();

        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackCooldown;
            BeginAttackLock();
            
            animator.SetTrigger("DoAttack");
            Invoke("AttackWindow_Start",attackHitboxActivationDelay);
        }

        animator.SetFloat("Speed", 0f, 0.05f, Time.deltaTime);
        animator.SetFloat("Direction", 0f, 0.05f, Time.deltaTime);
    }

    public HandHitbox handHitbox;

    public void AttackWindow_Start()
    {
        if (handHitbox) handHitbox.SetActive(true);
        Invoke("AttackWindow_End",0.5f);
    }

    public void AttackWindow_End()
    {
        if (handHitbox) handHitbox.SetActive(false);
    }
    void BeginAttackLock()
    {
        isAttacking = true;
        //AttackWindow_Start();
        attackUnlockTime = Time.time + attackLockTime; // fallback unlock
    }

    void EndAttackLock()
    {
        isAttacking = false;
        //AttackWindow_End();
        if (agent) agent.isStopped = false;
    }

    // Call from Animation Event at end of attack clip for perfect timing
    public void AnimEvent_EndAttack() => EndAttackLock();

    void TickSetDestination(Vector3 worldPos)
    {
        if (Time.time < nextUpdate) return;
        nextUpdate = Time.time + updatePathInterval;

        if (NavMesh.SamplePosition(worldPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }

    // NEW
    void PickNewWanderTarget()
    {
        hasWanderTarget = true;
        nextWanderPickTime = Time.time + Random.Range(wanderMinWait, wanderMaxWait);

        // random point around current position
        Vector3 random = Random.insideUnitSphere * wanderRadius;
        random.y = 0;
        Vector3 candidate = transform.position + random;

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            currentWanderTarget = hit.position;
        else
            currentWanderTarget = transform.position; // fallback
    }

    Vector3 ComputeFlockDirection()
    {
        Vector3 center = Vector3.zero;
        Vector3 separation = Vector3.zero;
        int count = 0;

        foreach (var other in flock)
        {
            if (other == this) continue;

            float d = Vector3.Distance(transform.position, other.transform.position);
            if (d <= neighborRadius)
            {
                center += other.transform.position;
                count++;

                if (d <= separationRadius && d > 0.001f)
                    separation += (transform.position - other.transform.position) / d;
            }
        }

        Vector3 dir = transform.forward;

        if (count > 0)
        {
            center /= count;
            Vector3 cohesion = (center - transform.position);
            cohesion.y = 0;

            separation.y = 0;

            dir = cohesion.normalized * cohesionWeight + separation.normalized * separationWeight;
        }

        // small random drift
        Vector3 drift = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        dir = (dir + drift * wanderWeight);
        dir.y = 0;

        return dir.sqrMagnitude < 0.0001f ? transform.forward : dir.normalized;
    }

    void UpdateAnimatorFromAgent()
    {
        if (agent.isStopped)
        {
            animator.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);
            animator.SetFloat("Direction", 0f, 0.1f, Time.deltaTime);
            return;
        }

        Vector3 v = agent.velocity; v.y = 0;
        float planar = v.magnitude;
        float normalizedSpeed = Mathf.Clamp01(planar / Mathf.Max(0.01f, agent.speed));
        animator.SetFloat("Speed", normalizedSpeed, 0.1f, Time.deltaTime);

        Vector3 desired = agent.desiredVelocity; desired.y = 0;
        if (desired.sqrMagnitude < 0.001f)
        {
            animator.SetFloat("Direction", 0f, 0.1f, Time.deltaTime);
            return;
        }

        desired.Normalize();
        float signedAngle = Vector3.SignedAngle(transform.forward, desired, Vector3.up);
        float dirParam = Mathf.Clamp(signedAngle / 90f, -1f, 1f);
        animator.SetFloat("Direction", dirParam, 0.08f, Time.deltaTime);
    }

    void FacePlayer()
    {
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0;
        if (toPlayer.sqrMagnitude < 0.0001f) return;

        Quaternion look = Quaternion.LookRotation(toPlayer.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 12f);
    }
}
