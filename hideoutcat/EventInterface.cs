using EFT.Hideout;
using hideoutcat.Pathfinding;
using System;

namespace hideoutcat
{
    public interface IPlayerEvents
    {
        public event Action<AreaData> AreaSelected;
        public event Action<AreaData> AreaLevelUpdated;
        public event Action PlayerPrepareWorkout;
        public event Action PlayerStopWorkout;
    }

    public static class CatDependencyProviders
    {
        public static Graph CatGraph { get; private set; }
        public static IPlayerEvents PlayerEvents { get; private set; }

        public static bool IsInitialized => CatGraph != null && PlayerEvents != null;

        public static void Initialize(Graph catGraph, IPlayerEvents playerEventsProvider)
        {
            if (IsInitialized)
            {
                UnityEngine.Debug.LogWarning($"{nameof(CatDependencyProviders)} already initialized.");
            }

            CatGraph = catGraph;
            PlayerEvents = playerEventsProvider;
        }
    }
}