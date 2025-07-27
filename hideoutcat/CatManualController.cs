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

            if (Input.GetKeyDown(KeyCode.Space))
            {
                cat.RequestJumpUp();
            }
        }
    }
}
