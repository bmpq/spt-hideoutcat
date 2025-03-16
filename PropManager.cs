using tarkin;
using Comfort.Common;
using EFT.Hideout;
using System.Linq;
using UnityEngine;

namespace hideoutcat
{
    internal static class PropManager
    {
        static GameObject herring;

        public static void Init()
        {
            PatchHideoutAwake.OnHideoutAwake += UpdateProps;
            PatchAreaSelected.OnAreaLevelUpdated += (_) => UpdateProps();
        }

        static void UpdateProps()
        {
            HideUnwantedSceneObjects();
            LoadProps();
        }

        static void LoadProps()
        {
            AreaData kitchenArea = Singleton<HideoutClass>.Instance.AreaDatas.FirstOrDefault(x => x.Template.Type == EFT.EAreaType.Kitchen);
            if (kitchenArea != null)
            {
                if (herring == null)
                {
                    herring = GameObject.Instantiate(AssetBundleLoader.LoadAssetBundle("hideoutcat_props").LoadAsset<GameObject>("herring_opened"));
                    AssetBundleLoader.ReplaceShadersToNative(herring);
                }

                herring.SetActive(kitchenArea.CurrentLevel > 0);
                switch (kitchenArea.CurrentLevel)
                {
                    case 1:
                        herring.transform.position = new Vector3(5.5347f, 0.848f, -5.6833f);
                        break;
                    case 2:
                    case 3:
                        herring.transform.position = new Vector3(5.494505f, 0.7590022f, -5.090755f);
                        break;
                }
            }
        }

        static void HideUnwantedSceneObjects()
        {
            AreaData heatingArea = Singleton<HideoutClass>.Instance.AreaDatas.FirstOrDefault(x => x.Template.Type == EFT.EAreaType.Heating);
            if (heatingArea != null)
            {
                switch (heatingArea.CurrentLevel)
                {
                    case 1:
                        heatingArea.HighlightTransform.Find("books_01 (1)")?.gameObject.SetActive(false);
                        break;
                    case 2:
                        heatingArea.HighlightTransform.Find("books_01 (2)")?.gameObject.SetActive(false);
                        break;
                    case 3:
                        heatingArea.HighlightTransform.Find("paper3 (1)")?.gameObject.SetActive(false);
                        heatingArea.HighlightTransform.Find("paper3 (2)")?.gameObject.SetActive(false);
                        heatingArea.HighlightTransform.Find("Firewood_4 (7)")?.gameObject.SetActive(false);
                        heatingArea.HighlightTransform.Find("Firewood_4 (6)")?.gameObject.SetActive(false);
                        break;
                }
            }

            AreaData kitchenArea = Singleton<HideoutClass>.Instance.AreaDatas.FirstOrDefault(x => x.Template.Type == EFT.EAreaType.Kitchen);
            if (kitchenArea != null)
            {
                switch (kitchenArea.CurrentLevel)
                {
                    case 1:
                        kitchenArea.HighlightTransform.Find("dish_1")?.gameObject.SetActive(false);
                        break;
                    case 2:
                        kitchenArea.HighlightTransform.Find("dish_1 (1)")?.gameObject.SetActive(false);
                        break;
                    case 3:
                        kitchenArea.HighlightTransform.Find("dish_1 (4)")?.gameObject.SetActive(false);
                        break;
                }
            }
        }
    }
}
