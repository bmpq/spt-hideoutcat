using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace hideoutcat.bepinex
{
    internal class PatchPlayerPrepareWorkout : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutPlayerOwner), nameof(HideoutPlayerOwner.PrepareWorkout));
        }

        [PatchPostfix]
        private static void PatchPostfix()
        {
            try
            {
                Plugin.PlayerEvents.TriggerPlayerWorkoutPrepare();
            }
            catch (Exception ex) { Plugin.Log.LogError(ex); }
        }
    }
}
