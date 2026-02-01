using UnityEngine;

public class AIIdleState : AIState
{
    float idleUntil;

    public AIIdleState(AIStateMachine sm, AIContext ctx) : base(sm, ctx) { }

    public override void Enter()
    {
        idleUntil = Time.time + ctx.idleDuration;
        if (ctx.agent) ctx.agent.ResetPath();
        if (ctx.animator) ctx.animator.SetTrigger("DoIdle");
    }

    public override void Tick()
{
    if (!NavMeshAgentSafe.IsValid(ctx.agent))
        return;

    // rest of logic...
        float dist = DistToPlayer();
        if (dist <= ctx.attackRange) { sm.SwitchState(sm.Attack); return; }
        if (dist <= ctx.aggroRange)  { sm.SwitchState(sm.Chase);  return; }

        if (Time.time >= idleUntil)
            sm.SwitchState(sm.Wander);

        // keep anim calm
        if (ctx.animator)
        {
            ctx.animator.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);
            ctx.animator.SetFloat("Direction", 0f, 0.1f, Time.deltaTime);
        }
    }
}
