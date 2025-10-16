using BepInEx;
using BepInEx.Logging;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using tarkin.hideoutcat.bepinex.Patches;
using tarkin.hideoutcat.Persistent;
using tarkin.hideoutcat.scene;
using tarkin.hideoutcat.ui;
using UnityEngine;

namespace tarkin.hideoutcat.bepinex
{
    [BepInPlugin("com.tarkin.hideoutcat", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Log;

        private HideoutCat _catInstance;

        private void Start()
        {
            var forceLoad = (typeof(SceneryDisabler), typeof(CatUIDataProvider));
            Log = base.Logger;

            Log.LogError("prewarmed");
            AssetBundleLoader.LoadBundle("hideoutcat_coats");
            AssetBundleLoader.LoadBundle("hideoutcat");

            HideoutCat.OnCatSpawned += OnCatSpawned;
            HideoutCat.OnCatDestroyed += OnCatDestroyed;

            new PatchAreaSelected().Enable();
            new Patch_GetActionsClass_GetAvailableHideoutActions().Enable();
            new PatchPlayerPrepareWorkout().Enable();
            new PatchPlayerStopWorkout().Enable();

            new PatchBonusPanelUpdateView().Enable();

            new Patch_EnvironmentUI_Awake().Enable();

            new Patch_GameWorld_Awake().Enable();
            new Patch_GameWorld_Dispose().Enable();
            new Patch_LaserBeam_Awake().Enable();
            new Patch_LaserBeam_OnDestroy().Enable();

            new Patch_HideoutCameraFlashlight_SetState().Enable();

            new Patch_HideoutCustomizationScreen_Init().Enable();

            new Patch_Generic<CW2.Animations.PhysicsSimulator>(nameof(CW2.Animations.PhysicsSimulator.Awake)).Enable();
            Patch_Generic<CW2.Animations.PhysicsSimulator>.OnPostfix += (instance) => 
            {
                for (int i = 0; i < instance.transform.childCount; i++)
                {
                    instance.transform.GetChild(i).gameObject.SetActive(false);
                }
            };
        }

        private void OnCatSpawned(HideoutCat cat)
        {
            Log.LogInfo("HideoutCat instance spawned. Starting initialization sequence.");
            _catInstance = cat;

            var allCoats = AssetBundleLoader.LoadBundle("hideoutcat_coats").LoadAllAssets<Coat>();

            CatSaveData saveData = SaveFileManager.Read();

            var dataController = new CatPersistentDataController(allCoats);
            dataController.ApplySaveData(saveData);

            InteractableCat interactableReceiver = new GameObject("EFTInteractable").AddComponent<InteractableCat>();
            interactableReceiver.transform.SetParent(cat.transform, false);

            _catInstance.Initialize(dataController);

            CatUIDataProvider.Initialize(
                coatsLoader: () => allCoats,
                currentCoatLoader: () => _catInstance.PersistentData.CurrentCoat,
                coatApplier: ApplyCoat,
                currentCatNameLoader: () => _catInstance.PersistentData.CatName,
                catNameSetter: _catInstance.PersistentData.SetCatName,
                currentFoodBowlStateLoader: () => _catInstance.PersistentData.CurrentFoodBowl,
                catFeeder: OnFeed
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

        private void OnFeed(Item item)
        {
            FoodItemClass food = item as FoodItemClass;
            if (food == null) return;
            if (food.HealthEffectsComponent?.HealthEffects == null) return;

            if (food.HealthEffectsComponent.HealthEffects.TryGetValue(EHealthFactorType.Energy, out var effect))
            {
                float delta = effect.Value * food.FoodDrinkComponent.RelativeValue;

                _catInstance?.PersistentData?.AddFoodToBowl(delta);

                NotificationManagerClass.DisplayMessageNotification($"Added food to bowl!!! added {delta} energy");
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