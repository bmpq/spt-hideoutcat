using UnityEngine;

namespace hideoutcat
{
    public class CatEyelids : MonoBehaviour
    {
        const float angleClosed = 20f;
        const float angleOpen = -42f;

        Transform boneEyelidL;
        Transform boneEyelidR;

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

        void Start()
        {
            boneEyelidL = transform.Find("RootNode/Arm_Cat/Skeleton/root_bone_01/Spine_base_02/spine_02_03/spine_03_04/spine_04_05/spine_05_06/neck_07/head_08/eyelid.L_014");
            boneEyelidR = transform.Find("RootNode/Arm_Cat/Skeleton/root_bone_01/Spine_base_02/spine_02_03/spine_03_04/spine_04_05/spine_05_06/neck_07/head_08/eyelid.R_018");

            if (boneEyelidL == null)
            {
                Debug.LogError($"Error init {nameof(CatEyelids)}! cant find armature bone");
                return;
            }
        }

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