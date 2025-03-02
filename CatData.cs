using EFT;
using UnityEngine;

namespace hideoutcat
{
    [System.Serializable]
    public class AnimatorParameter
    {
        public string Name;
        public AnimatorControllerParameterType Type;
        public bool BoolValue;
        public float FloatValue;
        public int IntValue;

        public void Apply(Animator animator)
        {
            Plugin.Log.LogInfo($"Setting animator parameter {Name}");
            switch (Type)
            {
                case AnimatorControllerParameterType.Bool:
                    animator.SetBool(Name, BoolValue);
                    break;
                case AnimatorControllerParameterType.Float:
                    animator.SetFloat(Name, FloatValue);
                    break;
                case AnimatorControllerParameterType.Int:
                    animator.SetInteger(Name, IntValue);
                    break;
                case AnimatorControllerParameterType.Trigger:
                    animator.SetTrigger(Name);
                    break;
                default:
                    break;
            }
        }
    }
}
