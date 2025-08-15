using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace tarkin
{
    public static class AssetBundleLoader
    {
        private static Dictionary<string, AssetBundle> loadedAssetBundles = new Dictionary<string, AssetBundle>();

        public static AssetBundle LoadBundle(string filename)
        {
            string gameDirectory = Path.GetDirectoryName(Application.dataPath);
            string relativePath = Path.Combine(BepInEx.Paths.PluginPath, "tarkin", "bundles", filename);
            string fullPath = Path.Combine(gameDirectory, relativePath);

            string key = Path.GetFileName(fullPath);

            if (loadedAssetBundles.ContainsKey(key))
            {
                return loadedAssetBundles[key];
            }

            AssetBundle assetBundle = AssetBundle.LoadFromFile(fullPath);
            if (assetBundle == null)
            {
                return null;
            }

            loadedAssetBundles.Add(key, assetBundle);
            return assetBundle;
        }

        public static void ReplaceShadersToNative(GameObject go)
        {
            foreach (var rend in go.GetComponentsInChildren<Renderer>(true))
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