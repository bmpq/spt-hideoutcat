using EFT.Hideout;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using UnityEngine;

namespace hideoutcat
{
    internal class PatchAreaSelected : ModulePatch
    {
        public static event Action<AreaData> OnAreaSelected;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AreaScreenSubstrate), nameof(AreaScreenSubstrate.SelectArea));
        }

        [PatchPostfix]
        private static void PatchPostfix(AreaData areaData)
        {
            OnAreaSelected?.Invoke(areaData);
        }
    }
}
