using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace tarkin.hideoutcat
{
    [CreateAssetMenu(fileName = "CoatGrey", menuName = "ScriptableObjects/Cat Coat")]
    public class Coat : ScriptableObject
    {
        public string Id;
        public string Label;
        public Texture2D MainTexture;
        [TextArea]
        public string Description;
        public Sprite Icon;

#if UNITY_EDITOR
        void Awake() 
        { 
            Id = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this));
        }
#endif
    }
}
