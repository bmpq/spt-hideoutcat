using tarkin.hideoutcat.States;
using UnityEngine;

namespace tarkin.hideoutcat
{
    internal class CatLocomotion : MonoBehaviour
    {
        [SerializeField] Animator animator;

        [SerializeField] CatJumpHandler jumpHandler;
        [SerializeField] CatGrounding grounding;

        public enum LocomotionState
        {
            Grounded,
            Jumping,
            Landing
        }
        public LocomotionState CurrentState { get; private set; }

        CatInput rawInput;
        CatInput smoothedInput;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        void RequestJumpUp()
        {
            if (CurrentState != LocomotionState.Grounded)
                return;

            bool ledgeFound = LedgeDetector.Detect(transform, jumpHandler.LedgeDetectConfig, out Vector3 ledgeCenter, out Vector3 ledgeForward);
            if (ledgeFound)
            {
                CurrentState = LocomotionState.Jumping;
                jumpHandler.InitiateJumpUp(ledgeCenter, ledgeForward);
            }
        }

        public void SetInput(CatInput input)
        {
            rawInput = input;

            if (input.RequestJumpUp)
            {
                RequestJumpUp();
            }
        }

        private void Update()
        {
            smoothedInput.Turn = Mathf.MoveTowards(smoothedInput.Turn, rawInput.Turn, Time.deltaTime * 5f);
            smoothedInput.Thrust = Mathf.MoveTowards(smoothedInput.Thrust, rawInput.Thrust, Time.deltaTime * 5f);
            smoothedInput.Crouch = Mathf.MoveTowards(smoothedInput.Crouch, rawInput.Crouch, Time.deltaTime * 5f);

            animator.SetFloat("Thrust", smoothedInput.Thrust);
            animator.SetFloat("Turn", smoothedInput.Turn);
            animator.SetFloat("Crouch", smoothedInput.Crouch);
        }

        // run after Animator
        void LateUpdate()
        {
            Vector3 finalPosition = transform.position;

            grounding.PerformIKAndCalculateHeightCorrection();

            switch (CurrentState)
            {
                case LocomotionState.Grounded:
                    if (grounding.ShouldFallForward)
                    {
                        CurrentState = LocomotionState.Jumping;
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
                        CurrentState = LocomotionState.Landing;
                    break;

                case LocomotionState.Landing:
                    if (jumpHandler.State == CatJumpHandler.JumpState.None)
                        CurrentState = LocomotionState.Grounded;
                    break;
            }

            transform.position = finalPosition;
        }
    }
}
