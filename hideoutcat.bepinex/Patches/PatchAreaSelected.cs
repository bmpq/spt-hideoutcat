using Comfort.Common;
using EFT.Hideout;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using tarkin.hideoutcat.ui.EFTDependent;

namespace tarkin.hideoutcat.bepinex
{
    internal class PatchAreaSelected : ModulePatch
    {
        public static event Action<AreaData> OnAreaSelected;
        public static event Action<AreaData> OnAreaUpdated;

        // when hideout unloads, all AreaDatas become obsolete, on the next hideout reload it'll just keep adding new instances
        // nothing breaks but it is a memory leak
        // todo: find a hook when hideout unloads to clear this dictionary
        static Dictionary<AreaData, Action> unsubscribeActions = new Dictionary<AreaData, Action>();

        static private CatAreaScreenSubstrate catAreaScreen;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AreaScreenSubstrate), nameof(AreaScreenSubstrate.SelectArea));
        }

        [PatchPostfix]
        private static void PatchPostfix(AreaScreenSubstrate __instance, AreaData areaData)
        {
            if (!unsubscribeActions.ContainsKey(areaData))
            {
                unsubscribeActions[areaData] = areaData.LevelUpdated.Subscribe((silent) => OnAreaUpdated?.Invoke(areaData));
            }

            if (catAreaScreen == null)
            {
                GameObject prefab = AssetBundleLoader.LoadBundle("ugui").LoadAsset<GameObject>("AreaScreenSubstrateCat");
                catAreaScreen = GameObject.Instantiate(prefab, __instance.transform.parent).GetComponent<CatAreaScreenSubstrate>();
            }

            bool kitchen = (areaData.Template.Type == EFT.EAreaType.Kitchen);

            if (kitchen)
                catAreaScreen.Display();
            else 
                catAreaScreen.Close();

            OnAreaSelected?.Invoke(areaData);
        }
    }
}
