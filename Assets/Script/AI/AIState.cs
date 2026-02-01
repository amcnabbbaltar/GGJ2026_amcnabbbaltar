using UnityEngine;

public abstract class AIState
{
    protected readonly AIStateMachine sm;
    protected readonly AIContext ctx;

    protected AIState(AIStateMachine sm, AIContext ctx)
    {
        this.sm = sm;
        this.ctx = ctx;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Tick() { }

    protected float DistToPlayer()
    {
        if (!ctx.player) return float.PositiveInfinity;
        return Vector3.Distance(ctx.transform.position, ctx.player.position);
    }

    protected void FacePlayer(float turnSpeed = 12f)
    {
        if (!ctx.player) return;
        Vector3 toPlayer = ctx.player.position - ctx.transform.position;
        toPlayer.y = 0;
        if (toPlayer.sqrMagnitude < 0.0001f) return;

        Quaternion look = Quaternion.LookRotation(toPlayer.normalized);
        ctx.transform.rotation = Quaternion.Slerp(ctx.transform.rotation, look, Time.deltaTime * turnSpeed);
    }

    protected void TickAnimatorFromAgent()
    {
        if (!ctx.agent || !ctx.animator) return;

        if (ctx.agent.isStopped)
        {
            ctx.animator.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);
            ctx.animator.SetFloat("Direction", 0f, 0.1f, Time.deltaTime);
            return;
        }

        Vector3 v = ctx.agent.velocity; v.y = 0;
        float planar = v.magnitude;
        float normalizedSpeed = Mathf.Clamp01(planar / Mathf.Max(0.01f, ctx.agent.speed));
        ctx.animator.SetFloat("Speed", normalizedSpeed, 0.1f, Time.deltaTime);

        Vector3 desired = ctx.agent.desiredVelocity; desired.y = 0;
        if (desired.sqrMagnitude < 0.001f)
        {
            ctx.animator.SetFloat("Direction", 0f, 0.1f, Time.deltaTime);
            return;
        }

        desired.Normalize();
        float signedAngle = Vector3.SignedAngle(ctx.transform.forward, desired, Vector3.up);
        float dirParam = Mathf.Clamp(signedAngle / 90f, -1f, 1f);
        ctx.animator.SetFloat("Direction", dirParam, 0.08f, Time.deltaTime);
    }

    protected void TickSetDestination(Vector3 worldPos)
    {
        if (!ctx.agent) return;
        if (Time.time < sm.NextUpdateTime) return;

        sm.NextUpdateTime = Time.time + ctx.updatePathInterval;

        if (UnityEngine.AI.NavMesh.SamplePosition(worldPos, out var hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
            ctx.agent.SetDestination(hit.position);
    }

    protected Vector3 ComputeFlockDirection()
    {
        Vector3 center = Vector3.zero;
        Vector3 separation = Vector3.zero;
        int count = 0;

        foreach (var other in AIFlockRegistry.Agents)
        {
            if (other == null || other == ctx) continue;

            float d = Vector3.Distance(ctx.transform.position, other.transform.position);
            if (d <= ctx.neighborRadius)
            {
                center += other.transform.position;
                count++;

                if (d <= ctx.separationRadius && d > 0.001f)
                    separation += (ctx.transform.position - other.transform.position) / d;
            }
        }

        Vector3 dir = ctx.transform.forward;

        if (count > 0)
        {
            center /= count;
            Vector3 cohesion = (center - ctx.transform.position);
            cohesion.y = 0;

            separation.y = 0;
            dir = cohesion.normalized * ctx.cohesionWeight + separation.normalized * ctx.separationWeight;
        }

        // drift
        Vector3 drift = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        dir = (dir + drift * ctx.wanderWeight);
        dir.y = 0;

        return dir.sqrMagnitude < 0.0001f ? ctx.transform.forward : dir.normalized;
    }
}
