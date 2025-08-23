using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using UnityEngine;

namespace tarkin.hideoutcat.bepinex.Patches
{
    internal class Patch_EnvironmentUI_Awake : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EnvironmentUI), nameof(EnvironmentUI.Awake));
        }

        [PatchPostfix]
        private static void PatchPostfix(EnvironmentUI __instance)
        {
            try
            {
                AssetBundleLoader.LoadBundle("hideoutcat_coats");
                AssetBundleLoader.LoadBundle("hideoutcat");
                SceneLoader.Instance.LoadBundleScene(AssetBundleLoader.LoadBundle("hideoutcat_mainmenu"), true);
            }
            catch (Exception e) { }
        }
    }
}
