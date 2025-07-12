using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace tarkin.hideoutcat.bepinex
{
    internal class PatchPlayerPrepareWorkout : ModulePatch
    {
        public static event Action OnPostfix;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutPlayerOwner), nameof(HideoutPlayerOwner.PrepareWorkout));
        }

        [PatchPostfix]
        private static void PatchPostfix()
        {
            OnPostfix?.Invoke();
        }
    }
}
