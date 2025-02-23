using EFT;
using EFT.Hideout;
using UnityEngine;

namespace hideoutcat
{
    public class HideoutCat : MonoBehaviourSingleton<HideoutCat>
    {
        Animator animator;
        EAreaType prevArea = EAreaType.NotSet;

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
            if (area.Template.Type == prevArea)
                return;
            prevArea = area.Template.Type;

            animator.SetBool("Defecating", area.Template.Type == EAreaType.WaterCloset);
            animator.SetBool("Sleeping", Random.value < 0.3f);
            animator.SetBool("LyingSide", false);
            animator.SetBool("LyingBelly", false);
            animator.SetBool("Sitting", false);
            animator.SetBool("Crouching", false);
            animator.SetBool("Eating", false);
            animator.ResetTrigger("Fidget");

            animator.ResetTrigger("Jump");
            animator.SetFloat("Thrust", 0);
            animator.SetFloat("Turn", 0);

            switch (area.Template.Type)
            {
                case EAreaType.WaterCloset:
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
                case EAreaType.Generator:
                    if (area.CurrentLevel == 3)
                    {
                        animator.SetBool("LyingSide", true);
                        animator.SetBool("Sleeping", true);
                        transform.localPosition = new Vector3(-1.164f, 1.6511f, -5.4177f);
                        transform.localEulerAngles = new Vector3(0, 314.79f, 0);
                    }
                    break;
                case EAreaType.IntelligenceCenter:
                    if (area.CurrentLevel == 2)
                    {
                        animator.SetBool("LyingSide", true);
                        transform.localPosition = new Vector3(0.7765f, 0.8618f, 1.545f);
                        transform.localEulerAngles = new Vector3(0, -173.26f, 0);
                    }
                    break;
                case EAreaType.BitcoinFarm:
                    if (area.CurrentLevel == 3)
                    {
                        animator.SetBool("LyingBelly", true);
                        transform.localPosition = new Vector3(1.158f, 0.93f, 0.326f);
                        transform.localEulerAngles = new Vector3(0, 179.3f, 0);
                    }
                    break;
                case EAreaType.SolarPower:
                    if (area.CurrentLevel == 1)
                    {
                        animator.SetBool("Sitting", true);
                        transform.localPosition = new Vector3(-2.1639f, 0.7264f, -5.0941f);
                        transform.localEulerAngles = new Vector3(0, -14.889f, 0);
                    }
                    break;
                case EAreaType.CircleOfCultists:
                    if (area.CurrentLevel == 1)
                    {
                        transform.localPosition = new Vector3(-8.783f, 2.839f, -17.707f);
                        transform.localEulerAngles = new Vector3(0, 268.734f, 0);
                        animator.SetFloat("Thrust", 3.6f); // max speed
                        animator.SetFloat("Turn", -1f);
                    }
                    break;
                case EAreaType.Kitchen:
                    if (area.CurrentLevel == 1)
                    {
                        transform.localPosition = new Vector3(5.7741f, 0.847f, -5.4869f);
                        transform.localEulerAngles = new Vector3(0, -139.41f, 0);
                        animator.SetBool("Eating", true);
                    }
                    else if (area.CurrentLevel == 2 || area.CurrentLevel == 3)
                    {
                        transform.localPosition = new Vector3(5.666f, 0.7508f, -5.333f);
                        transform.localEulerAngles = new Vector3(0, -33.965f, 0);
                        animator.SetBool("Eating", true);
                    }
                    break;
                case EAreaType.RestSpace:
                    animator.SetBool("Sleeping", true);
                    animator.SetBool("LyingSide", true);
                    if (area.CurrentLevel == 1)
                    {
                        transform.localPosition = new Vector3(15.663f, 0.2107f, -0.962f);
                        transform.localEulerAngles = new Vector3(0, 116.911f, 0);
                    }
                    else if (area.CurrentLevel == 2)
                    {
                        transform.localPosition = new Vector3(16.06f, 0.6086f, -0.483f);
                        transform.localEulerAngles = new Vector3(0, 263.607f, 0);
                    }
                    else if (area.CurrentLevel == 3)
                    {
                        transform.localPosition = new Vector3(15.8471f, 0.6587f, -0.5829f);
                        transform.localEulerAngles = new Vector3(0, 124.53f, 0);
                    }
                    break;
                case EAreaType.Gym:
                    if (area.CurrentLevel == 0)
                    {
                        if (Random.value < 0.1f) // todo: more location and behaviour variants for other areas
                        {
                            animator.SetFloat("Thrust", 3.6f); // max speed
                            animator.SetFloat("Turn", 1f);
                            transform.localPosition = new Vector3(10.155f, 0f, 10.693f);
                            transform.localEulerAngles = new Vector3(0, 87.277f, 0);
                        }
                        else
                        {
                            animator.SetBool("Sitting", true);
                            transform.localPosition = new Vector3(8.9117f, 1.9094f, 12.3574f);
                            transform.localEulerAngles = new Vector3(0, -164.199f, 0);
                        }
                    }
                    else if (area.CurrentLevel == 1)
                    {
                        animator.Play("LieBelly", 0, 0);
                        animator.SetBool("Sleeping", true);
                        animator.SetBool("LyingBelly", true);
                        if (Random.value > 0.5f)
                        {
                            transform.localPosition = new Vector3(10.886f, 1.895f, 12.626f);
                            transform.localEulerAngles = new Vector3(0, -100.212f, 0);
                        }
                        else
                        {
                            transform.localPosition = new Vector3(11.997f, 0.089f, 11.206f);
                            transform.localEulerAngles = new Vector3(0, -196.733f, 0);
                        }
                    }
                    break;

            }

            string instantStateOverride = "Idle";
            if (animator.GetBool("LyingSide"))
                instantStateOverride = animator.GetBool("Sleeping") ? "LieSideSleep" : "LieSide";
            else if (animator.GetBool("LyingBelly"))
                instantStateOverride = animator.GetBool("Sleeping") ? "LieBellySleep" : "LieBelly";

            animator.Play(instantStateOverride, 0, 0);
            animator.Update(0f);
        }
    }
}
