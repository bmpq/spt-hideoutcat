using tarkin.hideoutcat.States;
using UnityEngine;

namespace tarkin.hideoutcat
{
    internal class CatManualController : CatStateBase
    {
        [SerializeField] private string inputKeyAxisCrouch = "Joystick Axis 3";
        [SerializeField] private float inputCrouchFactor = -1f;
        [SerializeField] private string inputKeyAxisSprint = "Joystick Axis 3";
        [SerializeField] private float inputSprintFactor = 1f;
        [SerializeField] private string inputKeyAxisJump = "Jump";

        void Start() { }

        public override void OnEnterState()
        {
        }

        public override void OnExitState()
        {
        }

        public override StateTickResult Tick(float _)
        {
            if (!isActiveAndEnabled)
                return StateTickResult.StateDone;

            CatInput input = new CatInput();
            input.Thrust = Input.GetAxis("Vertical");

            if (Input.GetKey(KeyCode.LeftShift))
                input.Thrust *= 3.6f;
            else
            {
                input.Thrust += input.Thrust * Mathf.Lerp(0, 2.6f, Mathf.Clamp01(Input.GetAxis(inputKeyAxisSprint) * inputSprintFactor));
            }

            input.Turn = Input.GetAxis("Horizontal");
            input.RequestJumpUp = Input.GetKey(KeyCode.Space) || Input.GetAxis(inputKeyAxisJump) > 0.5f;
            input.Crouch = Input.GetKey(KeyCode.C) ? 1f : Mathf.Clamp01(Input.GetAxis(inputKeyAxisCrouch) * inputCrouchFactor);

            return new StateTickResult(input);
        }
    }
}
