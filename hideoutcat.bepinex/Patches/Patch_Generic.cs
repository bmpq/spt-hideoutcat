using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace tarkin.hideoutcat.bepinex.Patches
{
    internal class Patch_Generic<T> : ModulePatch
    {
        public static event Action<T> OnPostfix;
        public static event Action<T> OnPrefix;

        private readonly string targetMethod;

        public Patch_Generic(string methodSignature) : base()
        {
            targetMethod = methodSignature;
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(T), targetMethod);
        }

#pragma warning disable IDE0051
        [PatchPrefix]
        private static bool PatchPrefix(T __instance)
        {
            try
            {
                OnPrefix?.Invoke(__instance);
            }
            catch { }
            return true;
        }

        [PatchPostfix]
        private static void PatchPostfix(T __instance)
        {
            try
            {
                OnPostfix?.Invoke(__instance);
            }
            catch { }
        }
#pragma warning restore IDE0051
    }
}
