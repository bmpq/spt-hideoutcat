using tarkin.hideoutcat.States;
using UnityEngine;

namespace tarkin.hideoutcat
{
    internal class CatManualController : CatStateBase
    {
        void Start() { }

        public override void OnEnterState()
        {
        }

        public override void OnExitState()
        {
        }

        public override StateTickResult Tick()
        {
            if (!isActiveAndEnabled)
                return StateTickResult.StateDone;

            CatInput input = new CatInput();
            input.Thrust = Input.GetAxis("Vertical");

            if (Input.GetKey(KeyCode.LeftShift))
                input.Thrust *= 3.6f;

            input.Turn = Input.GetAxis("Horizontal");
            input.RequestJumpUp = Input.GetKeyDown(KeyCode.Space);
            input.Crouch = Input.GetKeyDown(KeyCode.LeftControl) ? 1f : 0f;

            return new StateTickResult(input);
        }
    }
}
