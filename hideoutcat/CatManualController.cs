using tarkin.hideoutcat.States;
using UnityEngine;

namespace tarkin.hideoutcat
{
    internal class CatManualController : MonoBehaviour
    {
        void Update()
        {
            CatInput input = new CatInput();
            input.Thrust = Input.GetAxis("Vertical");
            input.Turn = Input.GetAxis("Horizontal");
            input.RequestJumpUp = Input.GetKeyDown(KeyCode.Space);
            input.Crouch = Input.GetKeyDown(KeyCode.LeftControl) ? 1f : 0f;

            // not implemented
        }
    }
}
