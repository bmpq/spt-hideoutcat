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

            DrawDefaultInspector();
        }
    }
}