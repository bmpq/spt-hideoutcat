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
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                Converters = { new Vector3Converter() }
            };
            List<Node> nodeGraph = JsonConvert.DeserializeObject<List<Node>>(File.ReadAllText(filePath));
            CatGraph = new Graph(nodeGraph);


            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError("error loading cat config file");
            return false;
        }
    }

    private void InitConfiguration()
    {
    }
}