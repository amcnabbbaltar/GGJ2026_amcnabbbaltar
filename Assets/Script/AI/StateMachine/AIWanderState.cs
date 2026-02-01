using UnityEngine;
using UnityEngine.AI;

public class AIWanderState : AIState
{
    public AIWanderState(AIStateMachine sm, AIContext ctx) : base(sm, ctx) { }

    public override void Enter()
    {
        sm.HasWanderTarget = false;
    }

   public override void Tick()
    {
    if (!NavMeshAgentSafe.IsValid(ctx.agent))
        return;


        float dist = DistToPlayer();
        if (dist <= ctx.attackRange) { sm.SwitchState(sm.Attack); return; }
        if (dist <= ctx.aggroRange)  { sm.SwitchState(sm.Chase);  return; }

        // maybe idle
        if (Time.time >= sm.NextUpdateTime && Random.value < ctx.idleChancePerTick && ctx.agent.isStopped == false)
        {
            sm.SwitchState(sm.Idle);
            return;
        }

        // pick / refresh wander target
        bool reached =
            !ctx.agent.pathPending &&
            ctx.agent.hasPath &&
            ctx.agent.remainingDistance <= Mathf.Max(ctx.agent.stoppingDistance + 0.2f, 0.4f);

        if (!sm.HasWanderTarget || reached || Time.time >= sm.NextWanderPickTime)
            PickNewWanderTarget();

        Vector3 target = sm.CurrentWanderTarget;

        Vector3 flockDir = ComputeFlockDirection();
        if (flockDir.sqrMagnitude > 0.001f)
            target += flockDir * (ctx.wanderWeight * 3f);

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            ctx.agent.SetDestination(hit.position);

        TickAnimatorFromAgent();
    }

    void PickNewWanderTarget()
    {
        sm.HasWanderTarget = true;
        sm.NextWanderPickTime = Time.time + Random.Range(ctx.wanderMinWait, ctx.wanderMaxWait);

        Vector3 random = Random.insideUnitSphere * ctx.wanderRadius;
        random.y = 0;
        Vector3 candidate = ctx.transform.position + random;

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, ctx.wanderRadius, NavMesh.AllAreas))
            sm.CurrentWanderTarget = hit.position;
        else
            sm.CurrentWanderTarget = ctx.transform.position;
    }
}
