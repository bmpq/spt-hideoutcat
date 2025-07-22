using UnityEngine;
using tarkin.hideoutcat;

namespace tarkin.hideoutcat.editor
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

            cat.manualInput = input;

            if (Input.GetKey(KeyCode.Space))
            {
                cat.InitiateJump(transform.position + transform.forward + transform.up * 2f, transform.forward);
            }
        }
    }
}
