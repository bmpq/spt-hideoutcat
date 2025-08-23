using EFT;
using HarmonyLib;
using tarkin.hideoutcat;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace tarkin.hideoutcat.bepinex
{
    internal class Patch_GetActionsClass_GetAvailableHideoutActions : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GetActionsClass), nameof(GetActionsClass.GetAvailableHideoutActions));
        }

        [PatchPrefix]
        private static bool PatchPrefix(ref ActionsReturnClass __result, HideoutPlayerOwner owner, GInterface150 interactive)
        {
            InteractableCat cat = interactive as InteractableCat;
            if (cat == null)
                return true;

            __result = GetCatAvailableActions(cat, owner);

            return false;
        }

        public static ActionsReturnClass GetCatAvailableActions(InteractableCat cat, HideoutPlayerOwner owner)
        {
            ActionsReturnClass actionsReturnClass = new ActionsReturnClass
            {
                Actions = new List<ActionsTypesClass>()
            };

            actionsReturnClass.Actions.Add(new ActionsTypesClass
            {
                Name = "Pet",
                Action = new Action(delegate
                {
                    cat.Pet();
                    owner.Player.SetInteractInHands(EInteraction.ContainerOpenDefault);
                    owner.Player.MovementContext.ResetCanUsePropState();
                }),
                Disabled = !cat.IsPettable()
            });

            actionsReturnClass.Actions.Add(new ActionsTypesClass
            {
                Name = "Wake up",
                Action = new Action(delegate
                {
                    cat.WakeUp();
                    owner.Player.MovementContext.ResetCanUsePropState();
                }),
                Disabled = !cat.IsSleeping()
            });

            return actionsReturnClass;
        }
    }
}
