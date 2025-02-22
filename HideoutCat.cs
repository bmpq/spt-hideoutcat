using EFT.Hideout;
using UnityEngine;

namespace hideoutcat
{
    internal class HideoutCat : MonoBehaviourSingleton<HideoutCat>
    {
        Animator animator;

        public override void Awake()
        {
            base.Awake();
            animator = GetComponent<Animator>();
        }

        void FixedUpdate()
        {
            animator.SetFloat("Random", Random.value); // used for different variants of fidgeting
            if (Random.value < 0.002f) // on avg every 10 seconds @ 50 calls per sec (1/500)
            {
                animator.SetTrigger("Fidget");
            }
        }

        // todo: think of a better solution than this lol
        public void SetCurrentSelectedArea(AreaData area)
        {
            animator.SetBool("Defecating", area.Template.Type == EFT.EAreaType.WaterCloset);
            animator.SetBool("Sleeping", Random.value < 0.3f);
            animator.SetBool("LyingSide", false);
            animator.SetBool("LyingBelly", false);
            animator.SetBool("Sitting", false);
            animator.SetBool("Crouching", false);
            animator.ResetTrigger("Fidget");

            switch (area.Template.Type)
            {
                case EFT.EAreaType.WaterCloset:
                    if (area.CurrentLevel == 2)
                    {
                        transform.localPosition = new Vector3(-6.8886f, 0.5198f, 4.3919f);
                        transform.localEulerAngles = new Vector3(0, 85.08801f, 0);
                    }
                    else if (area.CurrentLevel == 3)
                    {
                        transform.localPosition = new Vector3(-6.854f, 0.5989f, 6.2104f);
                        transform.localEulerAngles = new Vector3(0, 85.08801f, 0);
                    }
                    break;
                case EFT.EAreaType.IntelligenceCenter:
                    if (area.CurrentLevel == 2)
                    {
                        animator.SetBool("LyingSide", true);
                        transform.localPosition = new Vector3(0.7765f, 0.8618f, 1.545f);
                        transform.localEulerAngles = new Vector3(0, -173.26f, 0);
                    }
                    break;
                case EFT.EAreaType.BitcoinFarm:
                    if (area.CurrentLevel == 3)
                    {
                        animator.SetBool("LyingBelly", true);
                        transform.localPosition = new Vector3(1.158f, 0.93f, 0.326f);
                        transform.localEulerAngles = new Vector3(0, 179.3f, 0);
                    }
                    break;
                case EFT.EAreaType.SolarPower:
                    if (area.CurrentLevel == 1)
                    {
                        animator.SetBool("Sitting", true);
                        transform.localPosition = new Vector3(-2.1639f, 0.7264f, -5.0941f);
                        transform.localEulerAngles = new Vector3(0, -14.889f, 0);
                    }
                    break;
            }
        }
    }
}
