using EFT;
using EFT.HealthSystem;
using EFT.Hideout;
using HarmonyLib;
using hideoutcat.Pathfinding;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using tarkin;
using Comfort.Common;

namespace hideoutcat
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
