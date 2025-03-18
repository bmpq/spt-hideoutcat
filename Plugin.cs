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
    internal static ConfigEntry<Coat> Coat;
    internal static ConfigEntry<Color> EyeColor;

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

            PatchHideoutAwake.OnHideoutAwake += SpawnCat;
            PatchAreaSelected.OnAreaLevelUpdated += (_) => SpawnCat();

            PropManager.Init();
        }
    }

    private void InitConfiguration()
    {
        Coat = Config.Bind("", "Coat", hideoutcat.Coat.GREY, "Applies on the next hideout load");
        EyeColor = Config.Bind("", "Eye colour", new Color(0.56f, 0.75f, 0.40f), "Applies on the next hideout load");
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

        GameObject catObject = GameObject.Instantiate(AssetBundleLoader.LoadAssetBundle("hideoutcat").LoadAsset<GameObject>("hideoutcat"));
        AssetBundleLoader.ReplaceShadersToNative(catObject);

        SkinnedMeshRenderer rend = catObject.GetComponentInChildren<SkinnedMeshRenderer>();
        rend.materials[1].color = EyeColor.Value;
        if (Coat.Value != (Coat)Coat.DefaultValue)
        {
            string textureName = $"MAINTEX_{Coat.Value.ToString().ToUpper()}";
            Texture2D coatTex = AssetBundleLoader.LoadAssetBundle("hideoutcat").LoadAsset<Texture2D>(textureName);
            if (coatTex != null)
            {
                rend.materials[0].mainTexture = coatTex;
            }
            else
            {
                Plugin.Log.LogError($"Error loading {Coat.Value} coat texture");
            }
        }

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
}