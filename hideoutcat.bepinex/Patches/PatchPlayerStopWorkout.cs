using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace hideoutcat.bepinex
{
    internal class PatchPlayerStopWorkout : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutPlayerOwner), nameof(HideoutPlayerOwner.StopWorkout));
        }

        [PatchPostfix]
        private static void PatchPostfix()
        {
            try
            {
                Plugin.PlayerEvents.TriggerPlayerWorkoutStop();
            }
            catch (Exception ex) { Plugin.Log.LogError(ex); }
        }
    }
}
