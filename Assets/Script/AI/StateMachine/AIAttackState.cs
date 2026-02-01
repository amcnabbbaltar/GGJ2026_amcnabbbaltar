using UnityEngine;

public class AIAttackState : AIState
{
    public AIAttackState(AIStateMachine sm, AIContext ctx) : base(sm, ctx) { }

    public override void Enter()
    {
        if (!NavMeshAgentSafe.IsValid(ctx.agent))
            return;

        ctx.agent.isStopped = true;
        ctx.agent.ResetPath();

        if (!sm.IsAttackingLocked && Time.time >= sm.NextAttackTime)
        {
            sm.NextAttackTime = Time.time + ctx.attackCooldown;
            BeginAttackLock();

            // Use shared sequence system
            if (ctx.sequence != null)
            {
                ctx.sequence.Play(
                    triggerName: ctx.attackTriggerName,
                    totalDuration: ctx.attackLockTime,
                    timeA: ctx.attackHitboxActivationDelay, onA: () => ctx.handHitbox.SetActive(true),
                    timeB: ctx.attackHitboxActivationDelay + ctx.hitboxActiveDuration, onB: () => ctx.handHitbox.SetActive(false),
                    onEnd: () => ctx.handHitbox.SetActive(false)
                );

            }
            else
            {
                // fallback: old behavior
                if (ctx.animator) ctx.animator.SetTrigger("DoAttack");
                ctx.Invoke(nameof(AIContext.AttackWindow_StartProxy), ctx.attackHitboxActivationDelay);
            }
        }
    }

    public override void Tick()
    {
        if (!NavMeshAgentSafe.IsValid(ctx.agent))
            return;

        float dist = DistToPlayer();

        if (!sm.IsAttackingLocked && dist > ctx.attackRange)
        {
            sm.SwitchState(dist <= ctx.aggroRange ? sm.Chase : sm.Wander);
            return;
        }

        FacePlayer();

        if (sm.IsAttackingLocked && Time.time >= sm.AttackUnlockTime)
            EndAttackLock();

        if (ctx.animator)
        {
            ctx.animator.SetFloat("Speed", 0f, 0.05f, Time.deltaTime);
            ctx.animator.SetFloat("Direction", 0f, 0.05f, Time.deltaTime);
        }
    }

    void BeginAttackLock()
    {
        sm.IsAttackingLocked = true;
        sm.AttackUnlockTime = Time.time + ctx.attackLockTime;
    }

    void EndAttackLock()
    {
        sm.IsAttackingLocked = false;
    }

    public void EndAttackLockFromAnim()
    {
        EndAttackLock();
    }
}
