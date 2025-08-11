using tarkin.hideoutcat.States;
using UnityEngine;

namespace tarkin.hideoutcat
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(CatJumpHandler))]
    [RequireComponent(typeof(CatGrounding))]
    [RequireComponent(typeof(CapsuleCollider))]
    internal class CatLocomotion : MonoBehaviour
    {
        Animator animator;

        CatJumpHandler jumpHandler;
        CatGrounding grounding;

        [SerializeField] LayerMask wallLayerMask = 1 << 12;
        [SerializeField] float skinWidth = 0.3f;
        CapsuleCollider capsuleCollider;

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
            jumpHandler = GetComponent<CatJumpHandler>();
            grounding = GetComponent<CatGrounding>();
            capsuleCollider = GetComponent<CapsuleCollider>();
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

        // MonoBehaviour.OnAnimatorMove() overrides animator root motion, if on the same game object with animator
        private void OnAnimatorMove()
        {
            if (jumpHandler.State == CatJumpHandler.JumpState.Airborne)
                return;

            Vector3 desiredMovement = animator.deltaPosition;

            if (desiredMovement.sqrMagnitude > 0)
            {
                Vector3 p1 = transform.position + capsuleCollider.center + Vector3.up * (capsuleCollider.height * 0.5f - capsuleCollider.radius);
                Vector3 p2 = transform.position + capsuleCollider.center - Vector3.up * (capsuleCollider.height * 0.5f - capsuleCollider.radius);
                float castDistance = desiredMovement.magnitude + skinWidth;

                if (Physics.CapsuleCast(p1, p2, capsuleCollider.radius, desiredMovement.normalized, out RaycastHit hit, castDistance, wallLayerMask))
                {
                    // allow the cat to slide along the wall
                    desiredMovement = Vector3.ProjectOnPlane(desiredMovement, hit.normal);
                }
            }

            transform.position += desiredMovement;
            transform.rotation *= animator.deltaRotation;
        }

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
