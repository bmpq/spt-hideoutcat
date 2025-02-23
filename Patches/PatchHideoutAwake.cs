using EFT;
using EFT.HealthSystem;
using EFT.Hideout;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace hideoutcat
{
    internal class PatchHideoutAwake : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AreasController), nameof(AreasController.HideoutAwake));
        }

        [PatchPostfix]
        private static void Postfix(AreasController __instance)
        {
            GameObject catObject = GameObject.Instantiate(BundleLoader.Load("hideoutcat").LoadAsset<GameObject>("hideoutcat"));

            catObject.AddComponent<HideoutCat>();

            ReplaceShadersToNative(catObject);
        }

        static void ReplaceShadersToNative(GameObject go)
        {
            Renderer[] rends = go.GetComponentsInChildren<Renderer>();

            foreach (var rend in rends)
            {
                foreach (var mat in rend.materials)
                {
                    Shader nativeShader = Shader.Find(mat.shader.name);
                    if (nativeShader != null)
                        mat.shader = nativeShader;
                }
            }
        }
    }
}
