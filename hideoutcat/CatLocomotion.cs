using tarkin.hideoutcat.States;
using UnityEngine;

namespace tarkin.hideoutcat
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(CatJumpHandler))]
    [RequireComponent(typeof(CatGrounding))]
    [RequireComponent(typeof(CharacterController))]
    internal class CatLocomotion : MonoBehaviour
    {
        Animator animator;
        CharacterController controller;

        CatJumpHandler jumpHandler;
        CatGrounding grounding;

        [SerializeField] float jumpEndTransitionLength = 0.5f;

        public enum LocomotionState
        {
            Grounded,
            Jumping,
            Landing
        }
        public LocomotionState CurrentState { get; private set; }

        private float timeSinceJumpEnded;

        CatInput rawInput;
        CatInput smoothedInput;

        CatInput microAdjustments;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            controller = GetComponent<CharacterController>();

            jumpHandler = GetComponent<CatJumpHandler>();
            grounding = GetComponent<CatGrounding>();
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
            smoothedInput.Turn = Mathf.MoveTowards(smoothedInput.Turn, rawInput.Turn + microAdjustments.Turn, Time.deltaTime * 5f);
            smoothedInput.Thrust = Mathf.MoveTowards(smoothedInput.Thrust, rawInput.Thrust + microAdjustments.Thrust, Time.deltaTime * 5f);
            smoothedInput.Crouch = Mathf.MoveTowards(smoothedInput.Crouch, rawInput.Crouch + microAdjustments.Crouch, Time.deltaTime * 5f);

            animator.SetFloat("Thrust", smoothedInput.Thrust);
            animator.SetFloat("Turn", smoothedInput.Turn);
            animator.SetFloat("Crouch", smoothedInput.Crouch);
        }

        // MonoBehaviour.OnAnimatorMove() overrides animator root motion, if on the same game object with animator
        private void OnAnimatorMove()
        {
            if (jumpHandler.State == CatJumpHandler.JumpState.Airborne)
                return;

            Vector3 desiredMovement = animator.deltaPosition;

            controller.Move(desiredMovement);

            transform.rotation *= animator.deltaRotation;
        }

        void LateUpdate()
        {
            microAdjustments = CatInput.MoveTowards(microAdjustments, CatInput.ToStop, Time.deltaTime);

            timeSinceJumpEnded += Time.deltaTime;

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
                        float factor = jumpEndTransitionLength <= 0f ? 1f : Mathf.Min(timeSinceJumpEnded / jumpEndTransitionLength, 1f);
                        grounding.AlignTiltToGround(factor);

                        controller.Move(grounding.HeightCorrectionOffsetNextFrame * factor);

                        if (!grounding.BackLimbContact && smoothedInput.Thrust < 0.8f && (grounding.GetPawDistanceToGround(2) > 0.05f || grounding.GetPawDistanceToGround(2) > 0.05f))
                            microAdjustments.Thrust = Mathf.MoveTowards(microAdjustments.Thrust, 1f, Time.deltaTime * 10f);
                    }
                    break;

                case LocomotionState.Jumping:
                    if (grounding.FrontLimbContact)
                        jumpHandler.OnTouchingGround();
                    jumpHandler.UpdateAirborneMovement();
                    controller.Move(jumpHandler.FrameMovement);
                    if (jumpHandler.State == CatJumpHandler.JumpState.Exiting)
                        CurrentState = LocomotionState.Landing;
                    break;

                case LocomotionState.Landing:
                    timeSinceJumpEnded = 0f;
                    if (jumpHandler.State == CatJumpHandler.JumpState.None)
                        CurrentState = LocomotionState.Grounded;
                    break;
            }
        }
    }
}
