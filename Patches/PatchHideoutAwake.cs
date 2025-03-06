﻿using EFT;
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

namespace hideoutcat
{
    internal class PatchHideoutAwake : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutController), nameof(HideoutController.HideoutAwake));
        }

        [PatchPostfix]
        private static void Postfix(HideoutController __instance)
        {
            GameObject catObject = GameObject.Instantiate(BundleLoader.Load("hideoutcat").LoadAsset<GameObject>("hideoutcat"));
            ReplaceShadersToNative(catObject);

            HideoutCat cat = catObject.AddComponent<HideoutCat>();

            List<HideoutArea> availableArea = new List<HideoutArea>();
            foreach (var kvp in __instance.Areas)
            {
                if (kvp.Value.Data.CurrentLevel > 0)
                    availableArea.Add(kvp.Value);
            }
            if (availableArea.Count > 0)
            {
                Plugin.Log.LogDebug($"{availableArea.Count} avaiable areas");

                availableArea.OrderBy(_ => System.Guid.NewGuid()).ToList();
                foreach (var spawnArea in availableArea)
                {
                    var nodes = Plugin.CatGraph.FindDeadEndNodesByAreaTypeAndLevel(spawnArea.AreaTemplate.Type, spawnArea.Data.CurrentLevel);
                    if (nodes.Count > 0)
                    {
                        Node target = nodes[Random.Range(0, nodes.Count)];
                        cat.transform.position = Plugin.CatGraph.GetNodeClosestWaypoint(target.position).position;
                        cat.SetTargetNode(target);
                        return;
                    }
                }
            }

            Plugin.Log.LogDebug("no available areas, defaulting to a random waypoint node");
            Node waypointNode = Plugin.CatGraph.GetNodeClosestWaypoint(new Vector3(Random.value * 16f, 0, 0));
            cat.transform.position = waypointNode.position;
            cat.SetTargetNode(waypointNode);
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
