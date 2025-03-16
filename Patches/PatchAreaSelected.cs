using EFT.Hideout;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace hideoutcat
{
    internal class PatchAreaSelected : ModulePatch
    {
        public static event Action<AreaData> OnAreaSelected;

        public static event Action<AreaData> OnAreaLevelUpdated;

        static Dictionary<AreaData, Action> unsubscribeActions = new Dictionary<AreaData, Action>();

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AreaScreenSubstrate), nameof(AreaScreenSubstrate.SelectArea));
        }

        [PatchPostfix]
        private static void PatchPostfix(AreaData areaData)
        {
            if (!unsubscribeActions.ContainsKey(areaData))
            {
                unsubscribeActions[areaData] = areaData.LevelUpdated.Subscribe((silent) => OnAreaLevelUpdated.Invoke(areaData));
            }

            OnAreaSelected?.Invoke(areaData);
        }
    }
}
