using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using tarkin.hideoutcat.Pathfinding;

namespace tarkin.hideoutcat.editor
{
    [CustomEditor(typeof(Node))]
    public class NodeInspector : Editor
    {
        private void OnEnable()
        {
            //catAnimator = FindAnimator();
        }

        public override void OnInspectorGUI()
        {
            Node node = (Node)target;

            if (Application.isPlaying)
            {
                if (GUILayout.Button("Set as destination", GUILayout.Height(50f)))
                {
                    FindObjectOfType<HideoutCat>().GoToNode(node);
                }

                EditorGUILayout.Space(10);
            }
            else
            {
                //TeleportCat(Node);
            }

            DrawDefaultInspector();
        }
    }
}