using EFT;
using UnityEngine;

namespace hideoutcat
{
    [System.Serializable]
    public class CatAreaAction
    {
        public EAreaType Area;
        public int AreaLevel;
        public AnimatorParameter[] Parameters;
        public Vector3 TransformPosition;
        public Vector3 TransformRotation;
        public float Chance;
    }

    [System.Serializable]
    public class AnimatorParameter
    {
        public string Name;
        public AnimatorControllerParameterType Type;
        public bool BoolValue;
        public float FloatValue;
        public int IntValue;
    }
}
