using tarkin.hideoutcat.Pathfinding;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

        [Space(10)]
        [SerializeField] float jumpUpEndOffsetY = 0.5f;
        [SerializeField] float jumpSpeed = 3.7f;
        [SerializeField] AnimationCurve jumpArcCurve;

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
            LandStart,
            LandEnd
        }

        public JumpState jumpState { get; private set; }
        Vector3 ledgePos;
        Vector3 ledgeDir;

        private Vector3 jumpStartPos;
        private float jumpProgress;

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

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Handles.Label(transform.position, jumpState.ToString());
        }
#endif

        void HandleJumping()
        {
            if (jumpState == JumpState.Airborne)
            {
                Vector3 landPos = ledgePos - new Vector3(0, jumpUpEndOffsetY, 0);

                float totalDistance = Vector3.Distance(jumpStartPos, landPos);

                if (totalDistance > 0.001f)
                {
                    float duration = totalDistance / jumpSpeed;
                    jumpProgress += Time.deltaTime / duration;
                }
                else
                {
                    jumpProgress = 1f;
                }

                if (jumpProgress < 1f)
                {
                    Vector3 currentPos = Vector3.Lerp(jumpStartPos, landPos, jumpProgress);
                    currentPos.y += jumpArcCurve.Evaluate(jumpProgress);
                    transform.position = currentPos;
                }
                else
                {
                    transform.position = landPos;

                    jumpState = JumpState.LandStart;
                    animator.SetBool("JumpingUp", false);
                }
            }
            else if (jumpState == JumpState.LandStart)
            {
                if (animator.IsInTransition(0) && JumpUpAir.Active && JumpUpEnd.Active)
                {
                    float t = animator.GetAnimatorTransitionInfo(0).normalizedTime;
                    Vector3 landPos = ledgePos - new Vector3(0, jumpUpEndOffsetY, 0);
                    transform.SetPositionIndividualAxis(y: Mathf.Lerp(landPos.y, ledgePos.y, t));
                }
                else
                {
                    jumpState = JumpState.LandEnd;
                }
            }
            else if (jumpState == JumpState.LandEnd)
            {
                transform.SetPositionIndividualAxis(y: ledgePos.y);

                if (!JumpUpEnd.Active)
                    jumpState = JumpState.None;
            }
        }

        public void InitiateJump(Vector3 ledgePos, Vector3 ledgeDir)
        {
            if (jumpState != JumpState.None)
                return;

            jumpState = JumpState.WindUp;

            this.ledgePos = ledgePos;
            this.ledgeDir = ledgeDir;

            animator.SetBool("JumpingUp", true);
        }

        // called from mecanim clip event
        public void TriggerAirborne()
        {
            jumpState = JumpState.Airborne;
            jumpStartPos = transform.position;
            jumpProgress = 0f;
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