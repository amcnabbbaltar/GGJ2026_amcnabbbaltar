using UnityEngine;
using UnityEngine.AI;

public static class NavMeshAgentSafe
{
    public static bool IsValid(NavMeshAgent agent)
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }
}

public class AIContext : MonoBehaviour
{
    [Header("Refs")]
    public NavMeshAgent agent;
    public Animator animator;
    public Transform player;
    public Health health;
    public Hitbox handHitbox;

    [Header("Reusable Sequence")]
    public ActionSequenceRunner sequence; // <-- add this (same component used by player)

    [Header("Ranges")]
    public float aggroRange = 10f;
    public float attackRange = 1.6f;

    [Header("Attack")]
    public float attackCooldown = 1.2f;
    public float attackLockTime = 0.7f;
    public float attackHitboxActivationDelay = 0.5f;
    public float hitboxActiveDuration = 0.5f;

    [Header("Attack Animator")]
    public string attackTriggerName = "DoAttack";

    [Header("Flocking")]
    public float neighborRadius = 6f;
    public float separationRadius = 1.5f;
    public float cohesionWeight = 1.0f;
    public float separationWeight = 1.5f;
    public float wanderWeight = 0.2f;

    [Header("Timing")]
    public float updatePathInterval = 0.2f;

    [Header("Idle")]
    public float idleChancePerTick = 0.02f;
    public float idleDuration = 1.0f;

    [Header("Wander")]
    public float wanderRadius = 8f;
    public float wanderMinWait = 1.0f;
    public float wanderMaxWait = 3.0f;

    void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        health = GetComponent<Health>();
        sequence = GetComponent<ActionSequenceRunner>();
    }

    void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!health) health = GetComponent<Health>();
        if (!sequence) sequence = GetComponent<ActionSequenceRunner>();

        if (agent)
        {
            agent.updateRotation = true;
            agent.angularSpeed = 360f;
        }

        // safety: hitbox off
        if (handHitbox) handHitbox.SetActive(false);
    }


    // ─────────────────────────────────────────────────────────────
    // Old proxy methods (still useful as fallback)
    // ─────────────────────────────────────────────────────────────
    public void AttackWindow_StartProxy()
    {
        if (handHitbox) handHitbox.SetActive(true);
        Invoke(nameof(AttackWindow_EndProxy), hitboxActiveDuration);
    }

    public void AttackWindow_EndProxy()
    {
        if (handHitbox) handHitbox.SetActive(false);
    }
}
