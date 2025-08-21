using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Linq;
using System.Reflection;
using tarkin.hideoutcat.ui.EFTDependent;
using UI.Hideout;
using UnityEngine;

namespace tarkin.hideoutcat.bepinex.Patches
{
    internal class Patch_HideoutCustomizationScreen_Init : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutCustomizationScreen), nameof(HideoutCustomizationScreen.Init));
        }

        [PatchPostfix]
        private static void PatchPostfix(
            HideoutCustomizationScreen __instance,
            Tab ____wallButton,
            HideoutCustomizationSimpleOptionsPanel ____simpleOptionPanel,
            HideoutCustomizationOptionsWithSlotsPanel ____optionWithSlotsPanel
        )
        {
            try
            {
                Transform panelParent = ____simpleOptionPanel.transform.parent;

                for (int i = 0; i < panelParent.childCount; i++)
                {
                    if (panelParent.GetChild(i).GetComponent<HideoutCustomizationCat>() != null)
                    {
                        // already spawned
                        return;
                    }
                }

                var catPanel = GameObject.Instantiate(AssetBundleLoader.LoadAsset<GameObject>("ugui", "CustomizationLayoutCat"), panelParent).GetComponent<HideoutCustomizationCat>();

                var tabCat = GameObject.Instantiate(AssetBundleLoader.LoadAsset<GameObject>("ugui", "CustomizationTabCat"), ____wallButton.transform.parent).GetComponent<Tab>();

                var originalTabs = ____wallButton.transform.parent.GetComponentsInChildren<Tab>().Where(t => t != tabCat).ToList();

                tabCat.OnSelectionChanged += (clickedTab, wantsToBeSelected) =>
                {
                    if (!wantsToBeSelected) return;

                    foreach (var otherTab in originalTabs)
                    {
                        otherTab.UpdateVisual(false);
                    }
                    tabCat.UpdateVisual(true);

                    ____simpleOptionPanel.Close();
                    ____optionWithSlotsPanel.Close();
                    catPanel.gameObject.SetActive(true);
                };
                tabCat.GetOrAddComponent<JankTabDisableFix>();

                foreach (var originalTab in originalTabs)
                {
                    originalTab.OnSelectionChanged += (clickedTab, wantsToBeSelected) =>
                    {
                        if (!wantsToBeSelected) return;
                        tabCat.UpdateVisual(false);
                        catPanel.gameObject.SetActive(false);
                    };
                }

                catPanel.gameObject.SetActive(false);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(ex);
            }
        }
    }
}
