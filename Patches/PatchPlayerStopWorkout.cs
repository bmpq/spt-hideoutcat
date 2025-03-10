using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace hideoutcat
{
    internal class PatchPlayerStopWorkout : ModulePatch
    {
        public static event Action OnPlayerStopWorkout;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutPlayerOwner), nameof(HideoutPlayerOwner.StopWorkout));
        }

        [PatchPostfix]
        private static void PatchPostfix()
        {
            try
            {
                OnPlayerStopWorkout?.Invoke();
            }
            catch (Exception ex) { Plugin.Log.LogError(ex); }
        }
    }
}
