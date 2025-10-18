#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

namespace tarkin.hideoutcat.scene
{
    public class SceneryDisablerWindow : EditorWindow
    {
        private SceneryDisabler disablerInstance;
        private SerializedObject serializedDisabler;
        private SerializedProperty pathsToDisableProp;

        private Vector2 scrollPosition;
        private int hiddenCount = 0;

        [MenuItem("Tools/Scenery Object Disabler")]
        public static void ShowWindow()
        {
            GetWindow<SceneryDisablerWindow>("Scenery Disabler");
        }

        private void OnEnable()
        {
            SceneVisibilityManager.visibilityChanged += UpdateHiddenStatus;
            Selection.selectionChanged += Repaint;
            EditorApplication.hierarchyChanged += FindDisablerAndRefresh;
            FindDisablerAndRefresh();
        }

        private void OnDisable()
        {
            SceneVisibilityManager.visibilityChanged -= UpdateHiddenStatus;
            Selection.selectionChanged -= Repaint;
            EditorApplication.hierarchyChanged -= FindDisablerAndRefresh;
        }

        private void FindDisablerAndRefresh()
        {
            var disablers = FindObjectsOfType<SceneryDisabler>();
            if (disablers.Length > 0)
            {
                disablerInstance = disablers[0];
                if (disablers.Length > 1) Debug.LogWarning($"[{nameof(SceneryDisablerWindow)}] Multiple instances of SceneryDisabler found. Using the first one: {disablerInstance.name}.");

                serializedDisabler = new SerializedObject(disablerInstance);
                pathsToDisableProp = serializedDisabler.FindProperty("pathsToDisable");
            }
            else
            {
                disablerInstance = null;
                serializedDisabler = null;
                pathsToDisableProp = null;
            }
            UpdateHiddenStatus();
            Repaint();
        }

        private void OnGUI()
        {
            if (disablerInstance == null)
            {
                EditorGUILayout.HelpBox("No SceneryDisabler found in the current scene. Please add the component to a GameObject.", MessageType.Warning);
                if (GUILayout.Button("Create Disabler Object"))
                {
                    var go = new GameObject("SceneryDisabler_Manager");
                    go.AddComponent<SceneryDisabler>();
                    FindDisablerAndRefresh();
                }
                return;
            }

            serializedDisabler.Update();

            EditorGUILayout.LabelField("Managing Disabler:", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(disablerInstance, typeof(SceneryDisabler), true);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(serializedDisabler.FindProperty("targetSceneName"));
            EditorGUILayout.PropertyField(serializedDisabler.FindProperty("destroyInstead"));

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Scene Visibility Sync", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            int totalCount = pathsToDisableProp.arraySize;
            EditorGUILayout.LabelField($"Status: {hiddenCount} / {totalCount} objects hidden.");

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
            if (GUILayout.Button("Hide All Targeted Objects"))
            {
                SyncVisibility(true);
            }
            GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
            if (GUILayout.Button("Show All Targeted Objects"))
            {
                SyncVisibility(false);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();


            EditorGUILayout.Space(10);

            GUI.backgroundColor = new Color(0.8f, 0.9f, 1f);
            EditorGUI.BeginDisabledGroup(Selection.gameObjects.Length == 0);
            if (GUILayout.Button($"Add {Selection.gameObjects.Length} Selected Object(s)", GUILayout.Height(30)))
            {
                AddSelectedObjects();
            }
            EditorGUI.EndDisabledGroup();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Objects to Disable", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            int indexToRemove = -1;
            for (int i = 0; i < pathsToDisableProp.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                SerializedProperty pathProp = pathsToDisableProp.GetArrayElementAtIndex(i);
                EditorGUILayout.LabelField(pathProp.stringValue);

                if (GUILayout.Button("Ping", GUILayout.Width(50)))
                {
                    GameObject obj = SceneryDisabler.FindObjectByPath(pathProp.stringValue);
                    if (obj != null)
                    {
                        if (SceneVisibilityManager.instance.IsHidden(obj))
                        {
                            SceneVisibilityManager.instance.Show(obj, true);
                        }
                        EditorGUIUtility.PingObject(obj);
                    }
                }

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    indexToRemove = i;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (indexToRemove != -1)
            {
                string pathToRemove = pathsToDisableProp.GetArrayElementAtIndex(indexToRemove).stringValue;
                GameObject obj = SceneryDisabler.FindObjectByPath(pathToRemove);
                if (obj != null)
                {
                    SceneVisibilityManager.instance.Show(obj, true);
                }
                pathsToDisableProp.DeleteArrayElementAtIndex(indexToRemove);
            }

            EditorGUILayout.EndScrollView();

            if (serializedDisabler.ApplyModifiedProperties())
            {
                UpdateHiddenStatus();
            }
        }

        private void SyncVisibility(bool hide)
        {
            if (pathsToDisableProp == null) return;

            Undo.RecordObject(SceneVisibilityManager.instance, hide ? "Hide Targeted Objects" : "Show Targeted Objects");

            for (int i = 0; i < pathsToDisableProp.arraySize; i++)
            {
                string path = pathsToDisableProp.GetArrayElementAtIndex(i).stringValue;
                GameObject obj = SceneryDisabler.FindObjectByPath(path);
                if (obj != null)
                {
                    if (hide)
                    {
                        SceneVisibilityManager.instance.Hide(obj, true);
                    }
                    else
                    {
                        SceneVisibilityManager.instance.Show(obj, true);
                    }
                }
            }
            UpdateHiddenStatus();
        }

        private void UpdateHiddenStatus()
        {
            if (disablerInstance == null || pathsToDisableProp == null)
            {
                hiddenCount = 0;
                return;
            }

            int count = 0;
            for (int i = 0; i < pathsToDisableProp.arraySize; i++)
            {
                string path = pathsToDisableProp.GetArrayElementAtIndex(i).stringValue;
                GameObject obj = SceneryDisabler.FindObjectByPath(path);
                if (obj != null && SceneVisibilityManager.instance.IsHidden(obj))
                {
                    count++;
                }
            }
            hiddenCount = count;
            Repaint();
        }

        private void AddSelectedObjects()
        {
            Undo.RecordObject(disablerInstance, "Add objects to disabler list");

            int addedCount = 0;
            foreach (var go in Selection.gameObjects)
            {
                string path = GenerateHierarchyPath(go);
                if (!PathExists(path))
                {
                    int newIndex = pathsToDisableProp.arraySize;
                    pathsToDisableProp.InsertArrayElementAtIndex(newIndex);
                    pathsToDisableProp.GetArrayElementAtIndex(newIndex).stringValue = path;
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                Debug.Log($"Added {addedCount} new object path(s) to the disabler list.");
                EditorSceneManager.MarkSceneDirty(disablerInstance.gameObject.scene);
            }

            SyncVisibility(true);
        }

        private bool PathExists(string path)
        {
            for (int i = 0; i < pathsToDisableProp.arraySize; i++)
            {
                if (pathsToDisableProp.GetArrayElementAtIndex(i).stringValue == path) return true;
            }
            return false;
        }

        private string GenerateHierarchyPath(GameObject obj)
        {
            if (obj == null) return string.Empty;
            return string.Join("/", obj.GetComponentsInParent<Transform>().Reverse().Select(t => t.name));
        }
    }
}
#endif