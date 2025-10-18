using UnityEngine;

namespace tarkin.hideoutcat
{
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    public class CatPupils : MonoBehaviour
    {
        private Material matEyeInstance;

        [SerializeField, Range(0f, 1f)]
        private float targetValue = 0.3f;

        private float internalValue;

        private static readonly int DilationProperty = Shader.PropertyToID("_Dilation");

        void Awake()
        {
            var renderer = GetComponent<SkinnedMeshRenderer>();

            if (renderer.materials.Length > 1)
            {
                matEyeInstance = renderer.materials[1];
            }
            else
            {
                Debug.LogError("The SkinnedMeshRenderer on " + gameObject.name + " needs at least 2 materials.", this);

                enabled = false;
            }
        }

        void Update()
        {
            internalValue = Mathf.Lerp(internalValue, targetValue, Time.deltaTime * 3f);
            matEyeInstance?.SetFloat(DilationProperty, internalValue);
        }

        public void SetDilation(float dilation)
        {
            targetValue = Mathf.Clamp01(dilation);
        }

        void OnDestroy()
        {
            if (matEyeInstance != null)
            {
                Destroy(matEyeInstance);
            }
        }
    }
}