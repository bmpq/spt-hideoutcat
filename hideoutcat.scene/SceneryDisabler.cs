using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

namespace tarkin.hideoutcat.scene
{
    public class SceneryDisabler : MonoBehaviour
    {
        public string targetSceneName = "bunker_2";

        [TextArea(5, 15)]
        public List<string> pathsToDisable;

        public bool destroyInstead = false;

        void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            CheckAndRunForAlreadyLoadedScene();
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void CheckAndRunForAlreadyLoadedScene()
        {
            Scene targetScene = SceneManager.GetSceneByName(targetSceneName);
            if (targetScene.isLoaded)
            {
                StartCoroutine(DisableObjectsRoutine());
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == targetSceneName)
            {
                StartCoroutine(DisableObjectsRoutine());
            }
        }

        private IEnumerator DisableObjectsRoutine()
        {
            // just in case
            yield return new WaitForEndOfFrame();

            Debug.Log($"[{nameof(SceneryDisabler)}] Processing {pathsToDisable.Count} paths...");
            int successCount = 0;

            foreach (string path in pathsToDisable)
            {
                GameObject targetObject = FindObjectByPath(path);

                if (targetObject != null)
                {
                    if (destroyInstead)
                        Destroy(targetObject);
                    else
                        targetObject.SetActive(false);

                    successCount++;
                }
                else
                {
                    Debug.LogWarning($"[{nameof(SceneryDisabler)}] Could not find object at path: {path}");
                }
            }

            if (destroyInstead)
            {
                Debug.Log($"[{nameof(SceneryDisabler)}] Finished processing. Successfully disabled {successCount}/{pathsToDisable.Count} objects.");
                Destroy(this);
            }
            else
            {
                Debug.Log($"[{nameof(SceneryDisabler)}] Finished processing. Successfully destroyed {successCount}/{pathsToDisable.Count} objects.");
                this.enabled = false;
            }
        }

        // a custom function instead of GameObject.Find()
        // for more control and to include inactive root objects in the search
        public static GameObject FindObjectByPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            // Standardize the path by removing a leading slash if it exists, just to be safe
            if (path.StartsWith("/"))
                path = path.Substring(1);

            string[] names = path.Split('/');

            GameObject root = null;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                foreach (var go in scene.GetRootGameObjects())
                {
                    if (go.name == names[0])
                    {
                        root = go;
                        break;
                    }
                }
                if (root != null) break;
            }

            if (root == null)
                return null;

            if (names.Length == 1)
                return root;

            Transform currentTransform = root.transform;
            for (int i = 1; i < names.Length; i++)
            {
                Transform child = currentTransform.Find(names[i]);
                if (child == null)
                {
                    return null;
                }
                currentTransform = child;
            }

            return currentTransform.gameObject;
        }
    }
}