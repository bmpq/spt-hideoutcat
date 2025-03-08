using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace hideoutcat
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

            __result = GetCatAvailableActions(cat);

            return false;
        }

        public static ActionsReturnClass GetCatAvailableActions(HideoutCat cat)
        {
            ActionsReturnClass actionsReturnClass = new ActionsReturnClass
            {
                Actions = new List<ActionsTypesClass>()
            };

            actionsReturnClass.Actions.Add(new ActionsTypesClass
            {
                Name = "Pet",
                Action = new Action(cat.Pet),
                Disabled = !cat.IsPettable()
            });

            return actionsReturnClass;
        }
    }
}
