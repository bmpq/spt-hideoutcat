using UnityEngine;
using UnityEditor;

namespace tarkin.hideoutcat.Environment
{
    [CustomEditor(typeof(FoodBowl))]
    public class FoodBowlEditor : Editor
    {
        private float _previewLevel;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            FoodBowl foodBowl = (FoodBowl)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Live Preview", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            _previewLevel = EditorGUILayout.Slider("Food Level", _previewLevel, 0f, 1f);

            if (EditorGUI.EndChangeCheck())
            {
                foodBowl.SetLevel(_previewLevel);
            }
        }
    }
}