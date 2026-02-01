using UnityEngine;

[RequireComponent(typeof(AIContext))]
[RequireComponent(typeof(AIStateMachine))]
public class AIDeathHandler : MonoBehaviour
{
    public float destroyDelay = 3f;

    AIContext ctx;

    void Awake()
    {
        ctx = GetComponent<AIContext>();
    }

    void OnEnable()
    {
        if (ctx.health != null)
            ctx.health.OnDied += HandleDeath;
    }

    void OnDisable()
    {
        if (ctx.health != null)
            ctx.health.OnDied -= HandleDeath;
    }

    void HandleDeath()
    {
        CancelInvoke();
        if (ctx.handHitbox) ctx.handHitbox.SetActive(false);

        if (ctx.agent)
        {
            ctx.agent.isStopped = true;
            ctx.agent.ResetPath();
            ctx.agent.enabled = false;
        }

        if (ctx.animator) ctx.animator.SetTrigger("Die");

        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        Destroy(gameObject, destroyDelay);
    }
}
