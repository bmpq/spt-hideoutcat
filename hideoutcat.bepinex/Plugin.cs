using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.Hideout;
using hideoutcat;
using hideoutcat.bepinex;
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

[BepInPlugin("com.tarkin.hideoutcat", "hideoutcat", "1.0.1.0")]
public class Plugin : BaseUnityPlugin
{
    internal static BepInExPlayerEvents PlayerEvents { get; private set; }

    internal static ConfigEntry<Coat> Coat;
    internal static ConfigEntry<Color> EyeColor;

    internal static new ManualLogSource Log;

    static bool catSpawned;

    private void Start()
    {
        Log = base.Logger;

        InitConfiguration();

        Graph catGraph = LoadCatAreaData();

        if (catGraph != null)
        {
            PlayerEvents = new BepInExPlayerEvents();
            CatDependencyProviders.Initialize(catGraph, PlayerEvents);

            new PatchHideoutAwake().Enable();
            new PatchAreaSelected().Enable();
            new PatchAvailableHideoutActions().Enable();
            new PatchPlayerPrepareWorkout().Enable();
            new PatchPlayerStopWorkout().Enable();

            new PatchBonusPanelUpdateView().Enable();

            PatchHideoutAwake.OnHideoutAwake += () => { catSpawned = false; SpawnCat(); };
            PlayerEvents.AreaLevelUpdated += (_) => SpawnCat();

            PropManager.Init();
        }
        else
        {
            Plugin.Log.LogError("Error loading Cat graph data!!!");
        }
    }

    private void InitConfiguration()
    {
        Coat = Config.Bind("", "Coat", hideoutcat.Coat.GREY, "Applies on the next hideout load");
        EyeColor = Config.Bind("", "Eye colour", new Color(0.56f, 0.75f, 0.40f), "Applies on the next hideout load");
    }

    private Graph LoadCatAreaData()
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
            return new Graph(nodes);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError("error loading cat config file: " + ex);
            return null;
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

        GameObject catObject = GameObject.Instantiate(AssetBundleLoader.LoadAssetBundle("hideoutcat").LoadAsset<GameObject>("hideoutcat"));
        //AssetBundleLoader.ReplaceShadersToNative(catObject);

        Plugin.Log.LogInfo("Cat spawned into scene!");

        catSpawned = true;

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

        AudioClip[] catAudioClips = AssetBundleLoader.LoadAssetBundle("hideoutcat_audio").LoadAllAssets<AudioClip>();
        if (catAudioClips == null || catAudioClips.Length == 0)
        {
            Debug.LogError("CatAudio: No audio clips loaded from bundle!");
        }
        else
        {
            CatAudio catAudio = catObject.AddComponent<CatAudio>();
            catAudio.Init(catAudioClips);
        }

        cat.TeleportToRandomWaypoint();
    }
}