using UnityEngine;

namespace tarkin.hideoutcat
{
    internal class CatManualController : MonoBehaviour
    {
        public HideoutCat cat;

        private Vector2 motion;

        void Update()
        {
            Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

            if (Input.GetKey(KeyCode.LeftShift))
            {
                input.y *= 3f;
            }

            cat.MovementInput = input;

            bool ledgeFound = LedgeDetector.Detect(transform, cat.LedgeDetectConfig, out Vector3 ledgeCenter, out Vector3 ledgeForward);

            if (ledgeFound && Input.GetKeyUp(KeyCode.Space))
            {
                cat.InitiateJump(ledgeCenter, ledgeForward);
            }
        }
    }
}
