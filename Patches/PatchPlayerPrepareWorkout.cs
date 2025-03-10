using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace hideoutcat
{
    internal class PatchPlayerPrepareWorkout : ModulePatch
    {
        public static event Action OnPlayerPrepareWorkout;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutPlayerOwner), nameof(HideoutPlayerOwner.PrepareWorkout));
        }

        [PatchPostfix]
        private static void PatchPostfix()
        {
            try
            {
                OnPlayerPrepareWorkout?.Invoke();
            }
            catch (Exception ex) { Plugin.Log.LogError(ex); }
        }
    }
}
