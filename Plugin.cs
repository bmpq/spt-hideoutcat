using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
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
}