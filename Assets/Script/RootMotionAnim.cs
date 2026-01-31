using UnityEngine;

public class RootMotionAnim : StateMachineBehaviour
{
    bool rootMotionValue ;
   
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //rootMotionValue = animator.applyRootMotion;
        animator.applyRootMotion = true;
        animator.gameObject.GetComponent<Rigidbody>().isKinematic = true;
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.applyRootMotion = false;
        animator.gameObject.GetComponent<Rigidbody>().isKinematic = false;
    }
}
