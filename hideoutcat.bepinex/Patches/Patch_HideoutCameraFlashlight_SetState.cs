using Comfort.Common;
using EFT;
using EFT.AssetsManager;
using EFT.Hideout;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace tarkin.hideoutcat.bepinex.Patches
{
    internal class Patch_HideoutCameraFlashlight_SetState : ModulePatch
    {
        private static GameObject laserEmitterObject;
        private static bool isCreatingLaser = false;

        private static bool cycle = false;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutCameraFlashlight), nameof(HideoutCameraFlashlight.SetState));
        }

        [PatchPrefix]
        private static bool PatchPrefix(HideoutCameraFlashlight __instance, bool active)
        {
            if (!active)
            {
                laserEmitterObject?.SetActive(false);
                return true;
            }

            if (!cycle) // on every other toggle on, it will turn on the laser pointer instead of the flashlight
            {
                cycle = true;
                return true;
            }

            if (laserEmitterObject == null && !isCreatingLaser)
            {
                CreateLaserEmitterAsync(__instance.gameObject.transform);
            }

            laserEmitterObject?.SetActive(true);

            cycle = false;

            return false;
        }

        private static async void CreateLaserEmitterAsync(Transform parent)
        {
            try
            {
                isCreatingLaser = true;
                string templateId = "56def37dd2720bec348b456a"; // tactical_all_surefire_x400_vis_laser

                Item item = Singleton<ItemFactoryClass>.Instance.CreateItem(MongoID.Generate(), templateId, null);
                ResourceKey[] resources = item.Template.AllResources.ToArray(); 
                await Singleton<PoolManagerClass>.Instance.LoadBundlesAndCreatePools(PoolManagerClass.PoolsCategory.Raid, PoolManagerClass.AssemblyType.Local, resources, JobPriorityClass.Immediate, null, PoolManagerClass.DefaultCancellationToken);

                var itemObject = Singleton<PoolManagerClass>.Instance.CreateItem(item, false);

                LaserBeam laser = itemObject.GetComponentInChildren<LaserBeam>(true);

                laserEmitterObject = GameObject.Instantiate(laser.gameObject);
                laserEmitterObject.transform.SetParent(parent, false);
                laserEmitterObject.transform.localPosition = new Vector3(0.2f, -0.3f, 0);
                laserEmitterObject.transform.localRotation = Quaternion.identity;
                laserEmitterObject.SetActive(true);

                itemObject.GetComponent<AssetPoolObject>().ReturnToPool();
            }
            catch (Exception e)
            {
                Debug.LogError($"error while creating the hideout laser pointer: {e.Message}");
                Debug.LogError(e.StackTrace);
            }
            finally
            {
                isCreatingLaser = false;
            }
        }
    }
}