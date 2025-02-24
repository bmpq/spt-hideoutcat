using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using DG.Tweening;
using EFT;
using hideoutcat;
using Newtonsoft.Json;
using Newtonsoft.Json.UnityConverters.Math;
using System;
using System.IO;
using UnityEngine;

[BepInPlugin("com.tarkin.hideoutcat", "hideoutcat", "1.0.0.0")]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Log;
    public static CatAreaAction[] CatConfig;

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
        string fileName = "CatAreaData.json";
        string filePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "BepInEx", "plugins", "tarkin", "bundles", fileName);

        try
        {
            string jsonString = File.ReadAllText(filePath);
            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                Converters = { new Vector3Converter() }
            };
            CatConfig = JsonConvert.DeserializeObject<CatAreaAction[]>(jsonString);
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError("cat config file not found at " + filePath);
            CatConfig = [];
            return false;
        }
    }

    private void InitConfiguration()
    {
    }
}