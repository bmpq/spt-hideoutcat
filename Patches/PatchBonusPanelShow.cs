using EFT.Hideout;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace hideoutcat
{
    internal class PatchBonusPanelUpdateView : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BonusPanel), nameof(BonusPanel.UpdateView));
        }

        [PatchPostfix]
        private static void PatchPostfix(SkillBonusAbstractClass ___skillBonusAbstractClass, TextMeshProUGUI ____description, TextMeshProUGUI ____effect, Image ____icon)
        {
            if (___skillBonusAbstractClass.Id.ToString() != "64f5b9e5fa34f11b380756d6")
                return;

            //____icon.sprite = sprite;
            ____description.text = "Unlocks cat";
            ____effect.text = "";
        }
    }
}
