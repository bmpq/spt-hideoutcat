using UnityEngine;

namespace hideoutcat
{
    internal class CatPupils : MonoBehaviour
    {
        Material matEye;

        float internalValue;
        float targetValue;

        void Start()
        {
            matEye = GetComponentInChildren<SkinnedMeshRenderer>().materials[1];
            targetValue = 0.3f;
        }

        void Update()
        {
            internalValue = Mathf.Lerp(internalValue, targetValue, Time.deltaTime * 3f);
            matEye.SetFloat("_Dilation", internalValue);
        }

        public void SetDilation(float dilation)
        {
            targetValue = dilation;
        }
    }
}
