using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using UnityEngine;

namespace tarkin.hideoutcat.bepinex.Patches
{
    internal class Patch_LaserBeam_Awake : ModulePatch
    {
        public static event Action<LaserBeam, Light> OnPostfix;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LaserBeam), nameof(LaserBeam.Awake));
        }

        [PatchPostfix]
        private static void PatchPostfix(LaserBeam __instance, Light ___light_0)
        {
            // the light_0 contains freshly spawned dot light, its position is set every frame in LaserBeam.LateUpdate

            try
            {
                OnPostfix?.Invoke(__instance, ___light_0);
            }
            catch (Exception ex) 
            {
                Debug.LogException(ex);
            }
        }
    }
}
