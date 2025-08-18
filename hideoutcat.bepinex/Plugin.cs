using BepInEx;
using BepInEx.Logging;
using tarkin.hideoutcat.bepinex.Patches;
using tarkin.hideoutcat.Persistent;
using tarkin.hideoutcat.ui;

namespace tarkin.hideoutcat.bepinex
{
    [BepInPlugin("com.tarkin.hideoutcat", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Log;

        private HideoutCat _catInstance;

        private void Start()
        {
            Log = base.Logger;

            HideoutCat.OnCatSpawned += OnCatSpawned;
            HideoutCat.OnCatDestroyed += OnCatDestroyed;

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

            new Patch_HideoutCustomizationScreen_Init().Enable();
        }

        private void OnCatSpawned(HideoutCat cat)
        {
            Log.LogInfo("HideoutCat instance spawned. Starting initialization sequence.");
            _catInstance = cat;

            var allCoats = AssetBundleLoader.LoadBundle("hideoutcat_coats").LoadAllAssets<Coat>();

            CatSaveData saveData = SaveFileManager.Read();

            var dataController = new CatPersistentDataController(allCoats);
            dataController.ApplySaveData(saveData);

            _catInstance.Initialize(dataController);

            CatUIDataProvider.Initialize(
                coatsLoader: () => allCoats,
                currentCoatLoader: () => _catInstance.PersistentData.CurrentCoat,
                coatApplier: ApplyCoat
            );

            Log.LogInfo("HideoutCat initialization complete.");
        }

        private void ApplyCoat(Coat coat)
        {
            if (_catInstance == null || coat == null) return;

            bool success = _catInstance.PersistentData.SetCoat(coat);
            if (success)
            {
                _catInstance.Appearance.ApplyCoatTexture(coat.MainTexture);
            }
        }

        private void OnCatDestroyed(HideoutCat cat)
        {
            CatSaveData dataToSave = cat.PersistentData.GetSaveData();
            SaveFileManager.Write(dataToSave);

            CatUIDataProvider.Reset();

            _catInstance = null;
        }
    }
}