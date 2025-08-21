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

        public static T LoadAsset<T>(string bundleName, string objectName) where T : Object
        {
            AssetBundle bundle = LoadBundle(bundleName);

            var allAssets = bundle.LoadAllAssets<T>();
            foreach (var item in allAssets)
            {
                if (item.name == objectName)
                {
                    return item;
                }
            }

            return null;
        }
    }
}