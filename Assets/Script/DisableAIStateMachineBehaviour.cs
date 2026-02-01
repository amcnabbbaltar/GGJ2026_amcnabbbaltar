using UnityEngine;
using UnityEngine.AI;

public class DisableAIMovementSMB : StateMachineBehaviour
{
    [Header("Options")]
    public bool stopAgent = true;
    public bool disableAgent = false; // use if you want FULL lock

    NavMeshAgent agent;
    Rigidbody rb;

    override public void OnStateEnter(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        agent = animator.GetComponent<NavMeshAgent>();
        rb = animator.GetComponent<Rigidbody>();

        if (agent)
        {
            if (disableAgent)
                agent.enabled = false;
            else if (stopAgent)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
        }

        if (rb)
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
    }

    override public void OnStateExit(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        if (agent)
        {
            if (disableAgent)
                agent.enabled = true;
            else
                agent.isStopped = false;
        }
    }
}
