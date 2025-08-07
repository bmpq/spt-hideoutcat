using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.Hideout;
using tarkin.hideoutcat.Pathfinding;
using System.Linq;
using UnityEngine;
using tarkin.hideoutcat.bepinex.Patches;

namespace tarkin.hideoutcat.bepinex
{
    [BepInPlugin("com.tarkin.hideoutcat", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ConfigEntry<Coat> Coat;
        internal static ConfigEntry<Color> EyeColor;

        internal static new ManualLogSource Log;

        static bool catSpawned;

        private void Start()
        {
            Log = base.Logger;

            InitConfiguration();

            new PatchAreaSelected().Enable();
            new PatchAvailableHideoutActions().Enable();
            new PatchPlayerPrepareWorkout().Enable();
            new PatchPlayerStopWorkout().Enable();

            new PatchBonusPanelUpdateView().Enable();

            new Patch_GameWorld_Awake().Enable();
            new Patch_GameWorld_Dispose().Enable();
            new Patch_LaserBeam_Awake().Enable();
            new Patch_LaserBeam_OnDestroy().Enable();

            new Patch_HideoutCameraFlashlight_SetState().Enable();
        }

        private void InitConfiguration()
        {
            Coat = Config.Bind("", "Coat", hideoutcat.Coat.GREY, "Applies on the next hideout load");
            EyeColor = Config.Bind("", "Eye colour", new Color(0.56f, 0.75f, 0.40f), "Applies on the next hideout load");
        }

        static bool RequirementsMet()
        {
            AreaData areaKitchen = Singleton<HideoutClass>.Instance.AreaDatas.FirstOrDefault(x => x.Template.Type == EFT.EAreaType.Kitchen);
            if (areaKitchen == null)
                return false;

            return areaKitchen.CurrentLevel > 0;
        }
    }
}