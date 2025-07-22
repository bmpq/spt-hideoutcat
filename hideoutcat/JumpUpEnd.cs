using UnityEngine;

namespace tarkin.hideoutcat
{
    internal class JumpUpEnd : StateMachineBehaviour
    {
        public static bool Active { get; private set; }

        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            Active = true;
        }

        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            Active = false;
        }
    }
}
