using tarkin.hideoutcat.Pathfinding;
using System.Collections.Generic;
using System.Linq;
using tarkin;
using UnityEngine;

namespace tarkin.hideoutcat
{
    public class HideoutCat : MonoBehaviour
    {
        [SerializeField] Animator animator;

        [SerializeField] CatLookAt lookAt;
        [SerializeField] CatEyelids eyelids;
        [SerializeField] CatPupils pupils;
        [SerializeField] CatAudio audio;

        [SerializeField] CatLimbsIK limbsIK;
        [SerializeField] CatGraphTraverser catGraphTraverser;

        private enum CatState
        {
            Idle,
            Moving,
            Sitting,
            Lying,
            Sleeping,
            Eating,
            Defecating,
            Sharpening,
            WaitingByDoor,
            Grooming
        }
        
        private CatState _currentState = CatState.Idle;
        private CatState _prevState;

        public Vector2 manualInput { get; set; }
        Vector2 movement;

        public enum JumpState
        {
            None,
            WindUp,
            Airborne,
            Landing
        }

        public JumpState jumpState { get; private set; }
        Vector3 ledgePos;
        Vector3 ledgeDir;

        public float jumpSpeed;

        void Update()
        {
            limbsIK.yControl = jumpState == JumpState.None;
            limbsIK.tiltFactor = jumpState == JumpState.None ? 1f : 0;

            if (jumpState == JumpState.None)
            {
                movement = Vector2.MoveTowards(movement, manualInput, Time.deltaTime * 5f);
                animator.SetFloat("Thrust", movement.y);
                animator.SetFloat("Turn", movement.x);
            }
            else
            {
                HandleJumping();
            }
        }

        void HandleJumping()
        {
            if (jumpState == JumpState.Airborne)
            {
                transform.position = Vector3.MoveTowards(transform.position, ledgePos, Time.deltaTime * jumpSpeed);
                if (transform.position == ledgePos)
                {
                    jumpState = JumpState.Landing;
                    animator.SetBool("JumpingUp", false);
                }
            }
            else if (jumpState == JumpState.Landing)
            {
                transform.rotation = Quaternion.LookRotation(ledgeDir);
                jumpState = JumpState.None;
            }
        }

        public void InitiateJump(Vector3 ledgePos, Vector3 ledgeDir)
        {
            jumpState = JumpState.WindUp;

            this.ledgePos = ledgePos;
            this.ledgeDir = ledgeDir;

            animator.SetBool("JumpingUp", true);
        }

        // called from mecanim clip event
        public void TriggerAirborne()
        {
            jumpState = JumpState.Airborne;
        }

        void ResetAnimatorParameters()
        {
            animator.SetBool("Defecating", false);
            animator.SetBool("Sleeping", false);
            animator.SetBool("LyingSide", false);
            animator.SetBool("LyingBelly", false);
            animator.SetBool("Sitting", false);
            animator.SetBool("Crouching", false);
            animator.SetBool("Eating", false);
            animator.SetBool("SharpeningHorizontal", false);
            animator.SetBool("SharpeningVertical", false);
            animator.SetBool("Grooming", false);
            animator.SetBool("RunningInCircles", false);
            animator.ResetTrigger("Fidget");
            animator.ResetTrigger("Meow");
            animator.ResetTrigger("Caress");
        }
    }
}