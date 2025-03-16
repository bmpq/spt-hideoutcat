using Comfort.Common;
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

        // when hideout unloads, all AreaDatas become obsolete, on the next hideout reload it'll just keep adding new instances
        // nothing breaks but it is a memory leak
        // todo: find a hook when hideout unloads to clear this dictionary
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
