using EFT.Hideout;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace tarkin.hideoutcat.bepinex
{
    internal class PatchHideoutAwake : ModulePatch
    {
        public static event Action OnPostfix;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutController), nameof(HideoutController.HideoutAwake));
        }

        [PatchPostfix]
        private static void Postfix(HideoutController __instance)
        {
            OnPostfix?.Invoke();
        }
    }
}
