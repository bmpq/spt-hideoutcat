using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using DG.Tweening;
using EFT;
using EFT.Hideout;
using hideoutcat;
using hideoutcat.Pathfinding;
using Newtonsoft.Json;
using Newtonsoft.Json.UnityConverters.Math;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using tarkin;
using UnityEngine;
using Random = UnityEngine.Random;

[BepInPlugin("com.tarkin.hideoutcat", "hideoutcat", "1.0.0.0")]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Log;
    public static Graph CatGraph;

    static bool catSpawned;

    private void Start()
    {
        Log = base.Logger;

        InitConfiguration();

        if (LoadCatAreaData())
        {
            new PatchHideoutAwake().Enable();
            new PatchAreaSelected().Enable();
            new PatchAvailableHideoutActions().Enable();
            new PatchPlayerPrepareWorkout().Enable();
            new PatchPlayerStopWorkout().Enable();

            new PatchBonusPanelUpdateView().Enable();

            PatchHideoutAwake.OnHideoutAwake += HideUnwantedSceneObjects;
            PatchAreaSelected.OnAreaLevelUpdated += (_) => HideUnwantedSceneObjects();

            PatchHideoutAwake.OnHideoutAwake += SpawnCat;
            PatchAreaSelected.OnAreaLevelUpdated += (_) => SpawnCat();
        }
    }

    private bool LoadCatAreaData()
    {
        try
        {
            string fileName = "CatNodeGraph.json";
            string filePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "BepInEx", "plugins", "tarkin", "bundles", fileName);
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                Converters = { new Vector3Converter() }
            };
            List<Node> nodes = JsonConvert.DeserializeObject<List<Node>>(File.ReadAllText(filePath));

            // resolve connections from string to class references
            foreach (var node in nodes)
            {
                foreach (var connectedName in node.connectedToNamesForSerialization)
                {
                    Node target = nodes.FirstOrDefault(n => n.name == connectedName);
                    if (target != null)
                        node.connectedTo.Add(target);
                    else
                        Plugin.Log.LogWarning($"Node '{node.name}': Connected node name '{connectedName}' not found in deserialized nodes.");
                }
                node.connectedToNamesForSerialization = null;
            }

            // we done
            CatGraph = new Graph(nodes);

            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError("error loading cat config file: " + ex);
            return false;
        }
    }

    private void InitConfiguration()
    {
    }

    static bool RequirementsMet()
    {
        AreaData areaKitchen = Singleton<HideoutClass>.Instance.AreaDatas.FirstOrDefault(x => x.Template.Type == EAreaType.Kitchen);
        if (areaKitchen == null)
            return false;

        return areaKitchen.CurrentLevel > 0;
    }

    static void SpawnCat()
    {
        if (catSpawned)
            return;

        if (!RequirementsMet())
            return;

        catSpawned = true;

        GameObject catObject = GameObject.Instantiate(BundleLoader.Load("hideoutcat").LoadAsset<GameObject>("hideoutcat"));
        ReplaceShadersToNative(catObject);

        HideoutCat cat = catObject.AddComponent<HideoutCat>();

        List<AreaData> availableArea = new List<AreaData>();
        foreach (var area in Singleton<HideoutClass>.Instance.AreaDatas)
        {
            if (area.CurrentLevel > 0)
                availableArea.Add(area);
        }
        if (availableArea.Count > 0)
        {
            Plugin.Log.LogInfo($"{availableArea.Count} avaiable areas");

            Random.InitState((int)System.DateTime.Now.Ticks); // apparently tarkov sets the seed somewhere, need to overwrite
            availableArea = availableArea.OrderBy(_ => Random.value).ToList();

            foreach (var spawnArea in availableArea)
            {
                var nodes = Plugin.CatGraph.FindDeadEndNodesByAreaTypeAndLevel(spawnArea.Template.Type, spawnArea.CurrentLevel);
                if (nodes.Count > 0)
                {
                    Node target = nodes[Random.Range(0, nodes.Count)];
                    cat.transform.position = Plugin.CatGraph.GetNodeClosestWaypoint(target.position).position;
                    cat.SetTargetNode(target);
                    return;
                }
            }
        }

        Plugin.Log.LogInfo("no available areas, defaulting to a random waypoint node");
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

    static void HideUnwantedSceneObjects()
    {
        // heating 1
        UnityExtensions.FindGameObjectWithComponentAtPosition<LODGroup>(new Vector3(14.38238f, 0.5160349f, -5.618773f))?.SetActive(false); // books_01 (1)

        // heating 2
        UnityExtensions.FindGameObjectWithComponentAtPosition<LODGroup>(new Vector3(14.20716f, 0.5158756f, -5.420396f))?.SetActive(false); // books_01 (2)

        // heating 3
        UnityExtensions.FindGameObjectWithComponentAtPosition<LODGroup>(new Vector3(15.85126f, 0.5397013f, -4.845883f))?.SetActive(false); // paper3 (1)
        UnityExtensions.FindGameObjectWithComponentAtPosition<LODGroup>(new Vector3(15.84810f, 0.5374010f, -5.039324f))?.SetActive(false); // paper3 (2)
        UnityExtensions.FindGameObjectWithComponentAtPosition<LODGroup>(new Vector3(15.97384f, 0.5497416f, -4.821522f))?.SetActive(false); // Firewood_4 (7)
        UnityExtensions.FindGameObjectWithComponentAtPosition<LODGroup>(new Vector3(16.07953f, 0.5244959f, -4.975954f))?.SetActive(false); // Firewood_4 (6)
    }
}