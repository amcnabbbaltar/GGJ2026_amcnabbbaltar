using UnityEngine;

public class LockMovementState : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        PlayerMovementLock.Instance?.LockMovement();
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        PlayerMovementLock.Instance?.UnlockMovement();
    }
}
