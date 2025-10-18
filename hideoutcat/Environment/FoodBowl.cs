using UnityEngine;

namespace tarkin.hideoutcat.Environment
{
    public class FoodBowl : MonoBehaviour
    {
        [SerializeField] private Animation anim;

        public void SetLevel(float level)
        {
            anim.Play();
            foreach (AnimationState state in anim)
            {
                state.normalizedTime = level;
                state.speed = 0;
            }
            anim.Sample();
        }
    }
}
