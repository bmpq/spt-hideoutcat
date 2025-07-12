using EFT;
using HarmonyLib;
using hideoutcat;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace tarkin.hideoutcat.bepinex
{
    internal class PatchAvailableHideoutActions : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GetActionsClass), nameof(GetActionsClass.GetAvailableHideoutActions));
        }

        [PatchPrefix]
        private static bool PatchPrefix(ref ActionsReturnClass __result, HideoutPlayerOwner owner, GInterface150 interactive)
        {
            HideoutCat cat = interactive as HideoutCat;
            if (cat == null)
                return true;

            __result = GetCatAvailableActions(cat, owner);

            return false;
        }

        public static ActionsReturnClass GetCatAvailableActions(HideoutCat cat, HideoutPlayerOwner owner)
        {
            ActionsReturnClass actionsReturnClass = new ActionsReturnClass
            {
                Actions = new List<ActionsTypesClass>()
            };

            return actionsReturnClass;
        }
    }
}
