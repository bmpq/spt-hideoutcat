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
            Node Node = (Node)target;

            if (Application.isPlaying)
            {
                if (GUILayout.Button("Set as destination", GUILayout.Height(50f)))
                {
                    //FindObjectOfType<Cat>().SetDestination(GraphEditor.instance.graph.FindNodeById(target.name));
                }

                EditorGUILayout.Space(10);
            }
            else
            {
                //TeleportCat(Node);
            }

            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            // --- Area and Level Detection and Display ---
            EditorGUILayout.LabelField("Area", EditorStyles.boldLabel);

            Transform parent = Node.transform.parent;
            Transform grandparent = parent ? parent.parent : null;

            // --- Level Detection ---
            int detectedLevel = -1; // Default to -1 to indicate not found
            if (parent != null && parent.name.StartsWith("level"))
            {
                string levelStr = parent.name.Substring("level".Length); // Extract the number part
                if (int.TryParse(levelStr, out int parsedLevel))
                {
                    detectedLevel = parsedLevel;
                }
            }
            // --- Area Type detection
            Transform searchParent = parent;
            EAreaType detectedAreaType = EAreaType.NotSet;
            while (searchParent != null && detectedAreaType == EAreaType.NotSet)
            {
                string areaString = Regex.Replace(searchParent.name, "[^a-zA-Z]", "");
                if (!Enum.TryParse<EAreaType>(areaString, true, out detectedAreaType))
                {
                    // enum parse fail sets to (0:Vents) and I need (-1:NotSet), so need to be explicit here:
                    detectedAreaType = EAreaType.NotSet;

                    searchParent = searchParent.parent;
                }
            }

            if (Node.areaLevel != detectedLevel)
            {
                Node.areaLevel = detectedLevel;
                EditorUtility.SetDirty(Node); // Mark as changed
            }

            if (Node.areaType != detectedAreaType)
            {
                Node.areaType = detectedAreaType;
                EditorUtility.SetDirty(Node); // Mark as changed
            }

            // --- Display Detected Values (Read-Only) ---
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField("Detected Level", detectedLevel);
            if (detectedLevel == -1)
            {
                EditorGUILayout.HelpBox("Could not detect level. Ensure this is intended", MessageType.Info);
            }

            EditorGUILayout.EnumPopup("Detected Area", detectedAreaType);
            if (detectedAreaType == EAreaType.NotSet)
            {
                EditorGUILayout.HelpBox("Could not detect Area Type. Setting to NotSet (waypoint)", MessageType.Info);
            }
            EditorGUI.EndDisabledGroup();

            UpdateNodeName(Node.gameObject, detectedAreaType, detectedLevel, Node.pose != Node.Pose.None);

            EditorGUILayout.Space(10);

            // --- Connections Section ---
            EditorGUILayout.LabelField("Outgoing connections", EditorStyles.boldLabel);

            if (Node.connectedTo.Count == 0)
            {
                EditorGUILayout.HelpBox("No outgoing connections.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < Node.connectedTo.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField("OutConnection " + (i + 1), Node.connectedTo[i], typeof(Node), true);
                    EditorGUI.EndDisabledGroup();

                    if (GUILayout.Button("Remove", GUILayout.Width(80)))
                    {
                        Node connectionToRemove = Node.connectedTo[i];

                        // 1. Record Undo for the current node
                        Undo.RecordObject(Node, "Remove Connection");
                        // 2. Remove connection from the current node's list
                        Node.connectedTo.RemoveAt(i);
                        EditorUtility.SetDirty(Node);

                        break; // Break to avoid index out of bounds
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        void TeleportCat(Node target)
        {
            throw new NotImplementedException();
        }

        void UpdateNodeName(GameObject gameObject, EAreaType area, int level, bool end)
        {
            const int idLength = 4; // 1 in 65536 chance of collision per generation. reason for truncating: 4 digits is easier to read than anything longer (lol)

            string uniqueid;
            if (gameObject.name.Length > idLength && gameObject.name[idLength] == '_') // already named
            {
                uniqueid = gameObject.name.Substring(0, idLength);
            }
            else
            {
                bool isUnique;

                do
                {
                    // the id has to be unique only among siblings, the chance of collision is negligible (I think)
                    uniqueid = GUID.Generate().ToString().Substring(0, idLength);

                    // check for uniqueness (do i need this)
                    isUnique = true; // assume unique until proven otherwise
                    for (int s = 0; s < gameObject.transform.parent.childCount; s++)
                    {
                        if (gameObject.transform.parent.GetChild(s).gameObject.name.StartsWith(uniqueid))
                        {
                            isUnique = false;
                            break;
                        }
                    }
                } while (!isUnique);
            }

            string newName;

            if (area == EAreaType.NotSet)
            {
                newName = $"{uniqueid}_Waypoint";
            }
            else
            {
                string levelString = level > -1 ? $"L{level}" : "AL";
                string pose = end ? "_POSE" : "";
                newName = $"{uniqueid}_{area}_{levelString}{pose}";
            }

            if (gameObject.name != newName)
                gameObject.name = newName;
        }

    }
}