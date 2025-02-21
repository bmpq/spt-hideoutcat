using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace hideoutcat
{
    public static class BundleLoader
    {
        private static Dictionary<string, AssetBundle> loadedAssetBundles = new Dictionary<string, AssetBundle>();

        public static AssetBundle Load(string filename)
        {
            string key = Path.GetFullPath(GetDefaultModAssetBundlePath(filename));

            if (loadedAssetBundles.ContainsKey(key))
                return loadedAssetBundles[key];

            AssetBundle assetBundle = AssetBundle.LoadFromFile(key);
            if (assetBundle == null)
            {
                Plugin.Log.LogError("Failed to load AssetBundle at path: " + key);
                return null;
            }

            loadedAssetBundles.Add(key, assetBundle);
            return assetBundle;
        }

        static string GetDefaultModAssetBundlePath(string filename)
        {
            string gameDirectory = Path.GetDirectoryName(Application.dataPath);
            string relativePath = Path.Combine("BepInEx", "plugins", "tarkin", "bundles", filename);
            string fullPath = Path.Combine(gameDirectory, relativePath);

            return fullPath;
        }
    }
}
