using System.Collections.Generic;
using System.Linq;
using tarkin.hideoutcat.bepinex.Patches;
using UnityEngine;

namespace tarkin.hideoutcat.bepinex
{
    internal class LasersManager : MonoBehaviour
    {
        private readonly List<Light> managedLaserDots = new List<Light>();

        private void OnEnable()
        {
            Patch_LaserBeam_Awake.OnPostfix += RegisterLaserDot;
            Patch_LaserBeam_OnDestroy.OnPostfix += UnregisterLaserDot;

            HideoutCat.OnRequestPotentialTargets += ProvideLaserTargets;
        }

        private void OnDisable()
        {
            Patch_LaserBeam_Awake.OnPostfix -= RegisterLaserDot;
            Patch_LaserBeam_OnDestroy.OnPostfix -= UnregisterLaserDot;

            HideoutCat.OnRequestPotentialTargets -= ProvideLaserTargets;
        }

        private void ProvideLaserTargets(List<Transform> targetList)
        {
            var activeDots = managedLaserDots
                .Where(dot => dot != null && dot.gameObject.activeInHierarchy && dot.intensity > 0f)
                .Select(dot => dot.transform);

            targetList.AddRange(activeDots);
        }

        private void RegisterLaserDot(LaserBeam beam, Light dot)
        {
            if (dot != null && !managedLaserDots.Contains(dot))
            {
                managedLaserDots.Add(dot);
            }
        }

        private void UnregisterLaserDot(LaserBeam beam, Light dot)
        {
            if (dot != null)
            {
                managedLaserDots.Remove(dot);
            }
        }
    }
}
