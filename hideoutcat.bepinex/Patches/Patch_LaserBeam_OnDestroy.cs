using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using UnityEngine;

namespace tarkin.hideoutcat.bepinex.Patches
{
    internal class Patch_LaserBeam_OnDestroy : ModulePatch
    {
        public static event Action<LaserBeam, Light> OnPostfix;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LaserBeam), nameof(LaserBeam.OnDestroy));
        }

        [PatchPostfix]
        private static void PatchPostfix(LaserBeam __instance, Light ___light_0)
        {
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
