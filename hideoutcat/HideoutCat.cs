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

        [SerializeField] CatGraphTraverser catGraphTraverser;

        [Space(10)]
        [SerializeField] LedgeDetector.Config ledgeDetectConfig = LedgeDetector.Config.Default;
        public LedgeDetector.Config LedgeDetectConfig => ledgeDetectConfig;
        [SerializeField] Vector2 jumpUpEndOffset = new Vector2(-0.2f, -0.4f);
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

        public Vector2 MovementInput { get; set; }
        Vector2 movement;

        public enum JumpState
        {
            None,
            WindUp,
            Airborne,
            AirborneDown,
            Land
        }

        public JumpState jumpState { get; private set; }
        Vector3 jumpTarget;
        Vector3 jumpForward;

        private Vector3 jumpStartPos;
        private float jumpProgress;

        private float jumpLandEndBlockTime;
        [SerializeField] private float jumpDownGravityFactor = 0.4f;
        [SerializeField] private float jumpDownForwardFactor = 0.3f;

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool showJumpState;
#endif

        void Update()
        {
            if (jumpState == JumpState.None)
            {
                movement = Vector2.MoveTowards(movement, MovementInput, Time.deltaTime * 5f);
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
            if (showJumpState)
            {
                Handles.Label(transform.position, jumpState.ToString());
            }
        }
#endif

        void HandleJumping()
        {
            if (jumpState == JumpState.Airborne)
            {
                float totalDistance = Vector3.Distance(jumpStartPos, jumpTarget);

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
                    Vector3 currentPos = Vector3.Lerp(jumpStartPos, jumpTarget, jumpProgress);
                    currentPos.y += jumpArcCurve.Evaluate(jumpProgress);
                    transform.position = currentPos;
                }
                else
                {
                    transform.position = jumpTarget;

                    jumpState = JumpState.Land;
                    animator.SetBool("JumpingUp", false);

                    jumpLandEndBlockTime = 1f;
                }
            }
            else if (jumpState == JumpState.AirborneDown)
            {
                transform.position += (Physics.gravity * jumpDownGravityFactor + transform.forward * jumpDownForwardFactor) * Time.deltaTime;
            }
            else if (jumpState == JumpState.Land)
            {
                jumpLandEndBlockTime -= Time.deltaTime;

                if (jumpLandEndBlockTime < 0)
                    jumpState = JumpState.None;
            }
        }

        public void InitiateJump(Vector3 ledgePos, Vector3 ledgeDir)
        {
            if (jumpState != JumpState.None)
                return;

            movement = Vector2.zero;

            jumpState = JumpState.WindUp;

            jumpTarget = ledgePos + ledgeDir * jumpUpEndOffset.x + new Vector3(0, jumpUpEndOffset.y, 0);
            jumpForward = ledgeDir;

            animator.SetBool("JumpingUp", true);

            float horizontalDistToLedge = 
                Mathf.Clamp01(
                    Vector3.Distance(
                        new Vector3(transform.position.x, 0, transform.position.z) + transform.forward * 0.3f, 
                        new Vector3(ledgePos.x, 0, ledgePos.z)
                    ) * 2f
                );
            // blend tree for the climbing animation based on the distance
            animator.SetFloat("Thrust", horizontalDistToLedge);
            animator.SetFloat("Turn", 0);
        }

        // called from mecanim clip event
        public void TriggerAirborne()
        {
            jumpState = JumpState.Airborne;
            jumpStartPos = transform.position;
            jumpProgress = 0f;
        }

        public void JumpDownStart()
        {
            jumpState = JumpState.WindUp;
            animator.SetBool("JumpingDown", true);
        }

        // called from mecanim clip event
        public void TriggerAirborneDown()
        {
            jumpState = JumpState.AirborneDown;
            jumpStartPos = transform.position;
            jumpProgress = 0f;
        }

        public void JumpDownEnd()
        {
            animator.SetBool("JumpingDown", false);
            jumpState = JumpState.Land;
            jumpLandEndBlockTime = 0.2f;
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