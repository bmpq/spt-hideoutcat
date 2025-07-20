using UnityEngine;
using tarkin.hideoutcat;

namespace tarkin.hideoutcat.editor
{
    internal class CatManualController : MonoBehaviour
    {
        public HideoutCat cat;

        private Animator animator;

        private Vector2 motion;

        void Start()
        {
            cat = FindObjectOfType<HideoutCat>();
            animator = cat.GetComponentInChildren<Animator>();
        }

        void Update()
        {
            Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

            if (Input.GetKey(KeyCode.LeftShift))
            {
                input.y *= 3f;
            }

            motion = Vector2.MoveTowards(motion, input, Time.deltaTime * 5f);

            animator.SetFloat("Thrust", motion.y);
            animator.SetFloat("Turn", motion.x);
        }
    }
}
