using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using DG.Tweening;
using EFT;
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

[BepInPlugin("com.tarkin.hideoutcat", "hideoutcat", "1.0.0.0")]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Log;
    public static Graph CatGraph;

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
        }

        PatchHideoutAwake.OnHideoutAwake += HideUnwantedSceneObjects;
        PatchAreaSelected.OnAreaLevelUpdated += (_) => HideUnwantedSceneObjects();
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