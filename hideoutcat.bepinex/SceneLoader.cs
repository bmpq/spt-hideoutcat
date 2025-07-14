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

        public void LoadBundleScene(string filename)
        {
            string fullPath = Path.Combine(BepInEx.Paths.PluginPath, "tarkin", "bundles", filename);

            StartCoroutine(LoadBundleSceneRoutine(fullPath));
        }

        IEnumerator LoadBundleSceneRoutine(string fullPath)
        {
            AssetBundleCreateRequest bundleRequest = AssetBundle.LoadFromFileAsync(fullPath);
            yield return bundleRequest;

            AssetBundle assetBundle = bundleRequest.assetBundle;
            if (assetBundle == null)
            {
                Debug.LogError($"Error loading asset bundle!");
                yield break;
            }

            string[] scenePaths = assetBundle.GetAllScenePaths();
            if (scenePaths.Length == 0)
            {
                Debug.LogError($"'{Path.GetFileName(fullPath)}' is not a scene bundle!");
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

            ReplaceShadersToNative(loadedScene);

            Debug.Log($"'{Path.GetFileName(fullPath)}': Scene loaded successfully.");

            assetBundle.Unload(false); // the false flag unloads the bundle file data, but keeps the loaded scene/assets in memory
        }

        private static void ReplaceShadersToNative(Scene loadedScene)
        {
            foreach (GameObject rootGameObject in loadedScene.GetRootGameObjects())
            {
                foreach (var rend in rootGameObject.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (var mat in rend.materials)
                    {
                        if (mat == null || mat.shader == null)
                            continue;

                        Shader nativeShader = Shader.Find(mat.shader.name);
                        if (nativeShader != null)
                        {
                            mat.shader = nativeShader;
                            Debug.Log($"Success finding native shader for {mat.shader.name} ({rend.gameObject.name})");
                        }
                        else
                            Debug.LogError($"Native shader '{mat.shader.name}' not found!");
                    }
                }
            }
        }
    }
}
