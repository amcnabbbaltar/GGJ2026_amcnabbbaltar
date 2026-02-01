using UnityEngine;

public class AIChaseState : AIState
{
    public AIChaseState(AIStateMachine sm, AIContext ctx) : base(sm, ctx) { }

    public override void Enter()
    {
        sm.HasWanderTarget = false;
        if (ctx.agent) ctx.agent.isStopped = false;
    }

    public override void Tick()
    {
        if (!NavMeshAgentSafe.IsValid(ctx.agent))
        return;

        float dist = DistToPlayer();
        if (dist <= ctx.attackRange) { sm.SwitchState(sm.Attack); return; }
        if (dist > ctx.aggroRange)   { sm.SwitchState(sm.Wander); return; }

        TickSetDestination(ctx.player.position);
        TickAnimatorFromAgent();
    }
}
