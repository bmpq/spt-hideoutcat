using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace tarkin.hideoutcat.bepinex.Patches
{
    internal class Patch_GameWorld_Dispose : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.Dispose));
        }

        [PatchPrefix]
        private static bool PatchPrefix(GameWorld __instance)
        {
            try
            {
            }
            catch { }
            return true;
        }
    }
}
