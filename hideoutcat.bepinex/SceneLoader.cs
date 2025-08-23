using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace tarkin.hideoutcat.bepinex
{
    internal class SceneLoader : MonoBehaviour
    {
        private static SceneLoader _instance;
        public static SceneLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject runnerObject = new GameObject("TarkinSceneLoader");
                    _instance = runnerObject.AddComponent<SceneLoader>();
                    DontDestroyOnLoad(runnerObject);
                }
                return _instance;
            }
        }

        public void LoadBundleScene(AssetBundle assetBundle, bool dontDestroyOnLoad = false)
        {
            StartCoroutine(LoadBundleSceneRoutine(assetBundle, dontDestroyOnLoad));
        }

        IEnumerator LoadBundleSceneRoutine(AssetBundle assetBundle, bool dontDestroyOnLoad)
        {
            if (assetBundle == null)
            {
                Debug.LogError($"Error loading asset bundle!");
                yield break;
            }

            string[] scenePaths = assetBundle.GetAllScenePaths();
            if (scenePaths.Length == 0)
            {
                Debug.LogError($"'{Path.GetFileName(assetBundle.name)}' is not a scene bundle!");
                assetBundle?.Unload(false);
                yield break;
            }

            string sceneName = Path.GetFileNameWithoutExtension(scenePaths[0]);

            if (SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                Debug.LogWarning($"Scene '{sceneName}' is already loaded!");
                yield break;
            }

            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

            Scene loadedScene = SceneManager.GetSceneByName(sceneName);
            if (!loadedScene.isLoaded)
            {
                Debug.LogError($"Failed to load scene '{sceneName}' from bundle.");
                assetBundle?.Unload(true);
                yield break;
            }

            Debug.Log($"'{Path.GetFileName(assetBundle.name)}': Scene loaded successfully.");

            if (dontDestroyOnLoad)
                DontDestroyOnLoadScene(loadedScene);

            assetBundle.Unload(false); // the false flag unloads the bundle file data, but keeps the loaded scene/assets in memory
        }

        private static void DontDestroyOnLoadScene(Scene loadedScene)
        {
            foreach (GameObject rootGameObject in loadedScene.GetRootGameObjects())
            {
                DontDestroyOnLoad(rootGameObject);
            }
        }
    }
}
