#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace tarkin.hideoutcat.scene
{
    [CustomEditor(typeof(SceneryDisabler))]
    public class SceneryDisablerEditor : Editor
    {
        private SerializedProperty propTargetSceneName;
        private SerializedProperty propPathsToDisable;
        private SerializedProperty propDestroyInstead;

        private ReorderableList reorderableList;

        private void OnEnable()
        {
            propTargetSceneName = serializedObject.FindProperty("targetSceneName");
            propPathsToDisable = serializedObject.FindProperty("pathsToDisable");
            propDestroyInstead = serializedObject.FindProperty("destroyInstead");

            reorderableList = new ReorderableList(serializedObject, propPathsToDisable,
                true, true, true, true);

            reorderableList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "GameObjects to Disable");
            };

            reorderableList.elementHeightCallback = (_) => EditorGUIUtility.singleLineHeight * 2 + 4;

            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                SerializedProperty element = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
                string path = element.stringValue;

                GameObject foundObject = SceneryDisabler.FindObjectByPath(path);

                rect.y += 2;
                rect.height = EditorGUIUtility.singleLineHeight;

                Color originalColor = GUI.backgroundColor;
                if (foundObject == null && !string.IsNullOrEmpty(path))
                {
                    GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                }

                EditorGUI.BeginChangeCheck();

                GameObject newObject = (GameObject)EditorGUI.ObjectField(rect, foundObject, typeof(GameObject), true);

                rect.y += EditorGUIUtility.singleLineHeight + 2;
                EditorGUI.LabelField(rect, path);

                GUI.backgroundColor = originalColor;

                if (EditorGUI.EndChangeCheck())
                {
                    if (newObject != null)
                    {
                        element.stringValue = GenerateHierarchyPath(newObject);
                    }
                    else
                    {
                        element.stringValue = "";
                    }
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(propTargetSceneName);
            EditorGUILayout.Space();

            propDestroyInstead.boolValue = EditorGUILayout.ToggleLeft("Fully destroy targets", propDestroyInstead.boolValue);
            EditorGUILayout.Space();

            reorderableList.DoLayoutList();

            HandleDragAndDrop(GUILayoutUtility.GetLastRect());

            serializedObject.ApplyModifiedProperties();
        }

        private void HandleDragAndDrop(Rect dropArea)
        {
            Event currentEvent = Event.current;
            EventType eventType = currentEvent.type;

            if (!dropArea.Contains(currentEvent.mousePosition))
                return;

            if (eventType == EventType.DragUpdated || eventType == EventType.DragPerform)
            {
                if (DragAndDrop.objectReferences.Any(obj => obj is GameObject))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;

                    if (eventType == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (var draggedObject in DragAndDrop.objectReferences)
                        {
                            if (draggedObject is GameObject go)
                            {
                                int newIndex = propPathsToDisable.arraySize;
                                propPathsToDisable.InsertArrayElementAtIndex(newIndex);
                                SerializedProperty newElement = propPathsToDisable.GetArrayElementAtIndex(newIndex);
                                newElement.stringValue = GenerateHierarchyPath(go);
                            }
                        }
                    }
                    currentEvent.Use();
                }
            }
        }

        private string GenerateHierarchyPath(GameObject obj)
        {
            if (obj == null) return "";

            StringBuilder sb = new StringBuilder();
            Transform current = obj.transform;

            List<string> pathParts = new List<string>();
            while (current != null)
            {
                pathParts.Add(current.name);
                current = current.parent;
            }

            for (int i = pathParts.Count - 1; i >= 0; i--)
            {
                sb.Append(pathParts[i]);
                if (i > 0) sb.Append("/");
            }

            return sb.ToString();
        }
    }
}
#endif