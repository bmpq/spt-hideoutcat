using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;

namespace tarkin.hideoutcat.bepinex.Patches
{
    internal class Patch_GameWorld_Awake : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.Awake));
        }

        [PatchPostfix]
        private static void PatchPostfix(GameWorld __instance)
        {
            try
            {
                SceneLoader.Instance.LoadBundleScene(AssetBundleLoader.LoadBundle("hideoutcat"));
                var lasersManager = new GameObject("Lasers Manager").AddComponent<LasersManager>();
            }
            catch { }
        }
    }
}
