using EFT.Hideout;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;

namespace hideoutcat
{
    internal class PatchAreaSelected : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AreaScreenSubstrate), nameof(AreaScreenSubstrate.SelectArea));
        }

        [PatchPostfix]
        private static void PatchPostfix(AreaData areaData)
        {
            if (!MonoBehaviourSingleton<HideoutCat>.Instantiated)
                return;

            MonoBehaviourSingleton<HideoutCat>.Instance.SetCurrentSelectedArea(areaData);

            return;
        }
    }
}
