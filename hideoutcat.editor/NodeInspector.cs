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