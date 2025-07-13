using UnityEngine;

namespace tarkin.hideoutcat
{
    public class CatEyelids : MonoBehaviour
    {
        [SerializeField] float angleClosed = 20f;
        [SerializeField] float angleOpen = -42f;

        [SerializeField] Transform boneEyelidL;
        [SerializeField] Transform boneEyelidR;

        float overrideValue;
        float maxValue;
        float internalValue;

        public enum Mode
        {
            None,
            Override,
            Clamp
        }
        float releasingTime;

        public Mode mode { get; private set; }

        // LateUpdate to override animator
        void LateUpdate()
        {
            float currentAngle = boneEyelidL.localEulerAngles.x;
            if (currentAngle > 180f)
                currentAngle -= 360f;
            float animatorValue = Mathf.InverseLerp(angleClosed, angleOpen, currentAngle);

            if (mode == Mode.None)
            {
                if (releasingTime < 1f)
                {
                    internalValue = Mathf.Lerp(internalValue, animatorValue, releasingTime);
                    releasingTime += Time.deltaTime * 2f;
                }
                else
                {
                    return;
                }
            }
            else if (mode == Mode.Override)
            {
                internalValue = Mathf.Lerp(internalValue, overrideValue, Time.deltaTime * 3f);
            }
            else if (mode == Mode.Clamp)
            {
                float clampedValue = Mathf.Clamp(animatorValue, 0, maxValue);
                internalValue = Mathf.Lerp(internalValue, clampedValue, Time.deltaTime * 3f);
            }

            float resultAngle = Mathf.Lerp(angleClosed, angleOpen, internalValue);
            boneEyelidL.localEulerAngles = new Vector3(resultAngle, 0, 0);
            boneEyelidR.localEulerAngles = new Vector3(resultAngle, 0, 0);
        }

        public void SetTarget(float openness)
        {
            mode = Mode.Override;
            overrideValue = openness;
        }

        public void SetClamp(float max)
        {
            mode = Mode.Clamp;
            maxValue = max;
        }

        public void Release()
        {
            if (mode == Mode.None)
                return;
            mode = Mode.None;
            releasingTime = 0;
        }
    }
}