using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace tarkin.hideoutcat
{
    [RequireComponent(typeof(Animator))]
    public class CatJumpHandler : MonoBehaviour
    {
        private Animator animator;

        [SerializeField] private float jumpUpExitDuration = 1.0f;
        [SerializeField] private float jumpDownExitDuration = 1.0f;

        [Space(10)]
        [SerializeField] LedgeDetector.Config ledgeDetectConfig = LedgeDetector.Config.Default; // won't deserialize and will fallback to default, without https://github.com/xiaoxiao921/FixPluginTypesSerialization
        public LedgeDetector.Config LedgeDetectConfig => ledgeDetectConfig;
        [SerializeField] Vector2 jumpUpEndOffset = new Vector2(-0.2f, -0.4f);
        [SerializeField] float jumpSpeed = 3.7f;
        [SerializeField] AnimationCurve jumpArcCurve;

        [Space(10)]
        [SerializeField] private float jumpDownGravityFactor = 0.4f;
        [SerializeField] private float jumpDownForwardFactor = 0.5f;

        private Vector3 jumpStartPos;
        private Vector3 jumpTarget;
        private float jumpProgress;

        private float jumpLandEndBlockTime;

        public enum JumpState
        {
            None,
            Entering,
            Airborne,
            Exiting
        }
        public JumpState State { get; private set; }
        public Vector3 FrameMovement { get; private set; }
        public bool DirectionUp { get; private set; }

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool showJumpState;
#endif

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void InitiateJumpUp(Vector3 ledgePos, Vector3 ledgeDir)
        {
            State = JumpState.Entering;
            DirectionUp = true;

            jumpTarget = ledgePos + ledgeDir * jumpUpEndOffset.x + new Vector3(0, jumpUpEndOffset.y, 0);

            animator.SetBool("JumpingUp", true);

            float horizontalDistToLedge =
                Mathf.Clamp01(
                    Vector3.Distance(
                        new Vector3(transform.position.x, 0, transform.position.z) + transform.forward * 0.3f,
                        new Vector3(ledgePos.x, 0, ledgePos.z)
                    ) * 2f
                );
            // blend tree for the climbing animation based on the distance
            animator.SetFloat("Distance", horizontalDistToLedge);
        }

        public void InitiateJumpDown()
        {
            State = JumpState.Entering;
            DirectionUp = false;
            animator.SetBool("JumpingDown", true);
        }

        public void OnTouchingGround()
        {
            if (State == JumpState.Airborne && !DirectionUp)
            {
                State = JumpState.Exiting;
                animator.SetBool("JumpingDown", false);
                jumpLandEndBlockTime = jumpDownExitDuration;
            }
        }

        // called from mecanim clip event
        public void OnAirborne()
        {
            State = JumpState.Airborne;
            jumpStartPos = transform.position;
            jumpProgress = 0f;
        }

        public void UpdateAirborneMovement()
        {
            // reset the movement delta each frame
            FrameMovement = Vector3.zero;

            if (State != JumpState.Airborne)
            {
                return;
            }

            if (DirectionUp)
            {
                float totalDistance = Vector3.Distance(jumpStartPos, jumpTarget);
                float duration = (totalDistance > 0.001f) ? totalDistance / jumpSpeed : 0f;
                jumpProgress += (duration > 0) ? Time.deltaTime / duration : 1f;

                if (jumpProgress < 1f)
                {
                    Vector3 newPos = Vector3.Lerp(jumpStartPos, jumpTarget, jumpProgress);
                    newPos.y += jumpArcCurve.Evaluate(jumpProgress);
                    FrameMovement = newPos - transform.position;
                }
                else
                {
                    FrameMovement = jumpTarget - transform.position;

                    animator.SetBool("JumpingUp", false);
                    State = JumpState.Exiting;
                    jumpLandEndBlockTime = jumpUpExitDuration;
                }
            }
            else
            {
                FrameMovement = (Physics.gravity * jumpDownGravityFactor + transform.forward * jumpDownForwardFactor) * Time.deltaTime;
            }
        }

        void Update()
        {
            if (State == JumpState.Exiting)
            {
                jumpLandEndBlockTime -= Time.deltaTime;
                if (jumpLandEndBlockTime < 0)
                    State = JumpState.None;
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (showJumpState)
            {
                Handles.Label(transform.position, State.ToString());
            }
        }
#endif
    }
}
