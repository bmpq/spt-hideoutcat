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

        [SerializeField] CatGraphTraverser graphTraverser;
        [SerializeField] CatJumpHandler jumpHandler;
        [SerializeField] CatGrounding grounding;

        public enum LocomotionState
        {
            Grounded,
            Jumping,
            Landing
        }
        private LocomotionState locomotionState;

        public Vector2 MovementInput { get; set; }
        Vector2 movement;
        public float CrouchInput { get; set; }
        float crouch;

        public void RequestJumpUp()
        {
            if (locomotionState != LocomotionState.Grounded)
                return;

            bool ledgeFound = LedgeDetector.Detect(transform, jumpHandler.LedgeDetectConfig, out Vector3 ledgeCenter, out Vector3 ledgeForward);
            if (ledgeFound)
            {
                locomotionState = LocomotionState.Jumping;
                jumpHandler.InitiateJumpUp(ledgeCenter, ledgeForward);
            }
        }

        void Update()
        {
            movement = Vector2.MoveTowards(movement, MovementInput, Time.deltaTime * 5f);
            crouch = Mathf.MoveTowards(crouch, CrouchInput, Time.deltaTime * 5f);

            animator.SetFloat("Thrust", movement.y);
            animator.SetFloat("Turn", movement.x);
            animator.SetFloat("Crouch", crouch);
        }

        // run after Animator
        void LateUpdate()
        {
            Vector3 finalPosition = animator.rootPosition;
            Quaternion finalRotation = animator.rootRotation;

            grounding.PerformIKAndCalculateHeightCorrection();

            switch (locomotionState)
            {
                case LocomotionState.Grounded:
                    if (grounding.ShouldFallForward)
                    {
                        locomotionState = LocomotionState.Jumping;
                        jumpHandler.InitiateJumpDown();
                    }
                    else
                    {
                        finalPosition += grounding.HeightCorrectionOffset;
                        grounding.AlignTiltToGround();
                    }
                    break;
                case LocomotionState.Jumping:
                    if (grounding.FrontLimbsContact)
                        jumpHandler.OnTouchingGround();
                    jumpHandler.CalculateNewPosition();
                    finalPosition = jumpHandler.CalculatedPosition;
                    if (jumpHandler.State == CatJumpHandler.JumpState.Exiting)
                        locomotionState = LocomotionState.Landing;
                    break;
                case LocomotionState.Landing:
                    if (jumpHandler.State == CatJumpHandler.JumpState.None)
                        locomotionState = LocomotionState.Grounded;
                    break;
            }

            transform.position = finalPosition;
            transform.rotation = finalRotation;
        }
    }
}