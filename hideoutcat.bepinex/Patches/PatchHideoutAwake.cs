using EFT.Hideout;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace hideoutcat.bepinex
{
    internal class PatchHideoutAwake : ModulePatch
    {
        public static event System.Action OnHideoutAwake;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutController), nameof(HideoutController.HideoutAwake));
        }

        [PatchPostfix]
        private static void Postfix(HideoutController __instance)
        {
            OnHideoutAwake?.Invoke();
        }
    }
}
