using UnityEngine;

[RequireComponent(typeof(AIContext))]
public class AIStateMachine : MonoBehaviour
{
    public AIContext Ctx { get; private set; }

    AIState current;

    [Header("Debug")]
    public bool debugStateChanges = true;
    public bool debugTick = false;
    public bool debugGuards = true;

    // shared timing across states
    public float NextUpdateTime { get; set; }

    // shared wander memory
    public float NextWanderPickTime { get; set; }
    public Vector3 CurrentWanderTarget { get; set; }
    public bool HasWanderTarget { get; set; }

    // shared attack memory
    public float NextAttackTime { get; set; }
    public bool IsAttackingLocked { get; set; }
    public float AttackUnlockTime { get; set; }

    // states
    public AIIdleState Idle { get; private set; }
    public AIWanderState Wander { get; private set; }
    public AIChaseState Chase { get; private set; }
    public AIAttackState Attack { get; private set; }

    void Awake()
    {
        Ctx = GetComponent<AIContext>();

        Idle   = new AIIdleState(this, Ctx);
        Wander = new AIWanderState(this, Ctx);
        Chase  = new AIChaseState(this, Ctx);
        Attack = new AIAttackState(this, Ctx);

        NextUpdateTime = Time.time + Random.Range(0f, Ctx.updatePathInterval);
        NextWanderPickTime = Time.time + Random.Range(Ctx.wanderMinWait, Ctx.wanderMaxWait);

        if (debugStateChanges)
            Debug.Log($"[{name}] AIStateMachine Awake");
    }

    void OnEnable()
    {
        AIFlockRegistry.Agents.Add(Ctx);

        if (debugStateChanges)
            Debug.Log($"[{name}] AIStateMachine Enabled");
    }

    void OnDisable()
    {
        AIFlockRegistry.Agents.Remove(Ctx);

        if (debugStateChanges)
            Debug.Log($"[{name}] AIStateMachine Disabled");
    }

    void Start()
    {
        if (debugStateChanges)
            Debug.Log($"[{name}] Initial State → Wander");

        SwitchState(Wander);
    }

    void Update()
    {
        if (Ctx.health != null && Ctx.health.IsDead)
        {
            if (debugGuards)
                Debug.Log($"[{name}] Update skipped (Dead)");
            return;
        }

        if (!Ctx.agent || !Ctx.agent.enabled || !Ctx.agent.isOnNavMesh)
        {
            if (debugGuards)
                Debug.Log($"[{name}] Update skipped (Agent invalid)");
            return;
        }

        if (debugTick)
            Debug.Log($"[{name}] Tick State = {current?.GetType().Name}");

        current?.Tick();
    }

    public void SwitchState(AIState next)
    {
        if (current == next)
            return;

        if (debugStateChanges)
        {
            string from = current != null ? current.GetType().Name : "None";
            string to = next != null ? next.GetType().Name : "None";
            Debug.Log($"[{name}] State Change: {from} → {to}");
        }

        current?.Exit();
        current = next;
        current?.Enter();
    }

    // ───────── Animation event passthrough ─────────
    public void AnimEvent_EndAttack()
    {
        if (debugStateChanges)
            Debug.Log($"[{name}] AnimEvent_EndAttack");

        Attack.EndAttackLockFromAnim();
    }
}
