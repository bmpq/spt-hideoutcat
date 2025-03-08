using System.Collections.Generic;
using UnityEngine;

namespace tarkin
{
    public static class UnityExtensions
    {
        public static GameObject FindGameObjectWithComponentAtPosition<T>(Vector3 position, float tolerance = 0.01f) where T : Component
        {
            T[] components = UnityEngine.Object.FindObjectsOfType<T>();

            foreach (T component in components)
            {
                if (component != null && Vector3.Distance(component.transform.position, position) <= tolerance)
                {
                    return component.gameObject;
                }
            }

            Plugin.Log.LogWarning($"GameObject with {typeof(T)} at position {position} not found");

            return null;
        }

        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            if (gameObject.TryGetComponent<T>(out T t))
            {
                return t;
            }
            else
            {
                return gameObject.AddComponent<T>();
            }
        }

        public static void SetPositionIndividualAxis(this Transform transform, float? x = null, float? y = null, float? z = null)
        {
            Vector3 pos = transform.position;
            if (x.HasValue) pos.x = x.Value;
            if (y.HasValue) pos.y = y.Value;
            if (z.HasValue) pos.z = z.Value;
            transform.position = pos;
        }

        public static void Shuffle<T>(this IList<T> ts)
        {
            var count = ts.Count;
            var last = count - 1;
            for (var i = 0; i < last; ++i)
            {
                var r = UnityEngine.Random.Range(i, count);
                var tmp = ts[i];
                ts[i] = ts[r];
                ts[r] = tmp;
            }
        }

        /// <summary>
        /// Checks if an event should occur based on its average interval and the elapsed time.
        /// </summary>
        /// <param name="avgIntervalSeconds">The average time between event occurrences.</param>
        /// <param name="deltaTime">The time elapsed since the last check.</param>
        public static bool RandomShouldOccur(float avgIntervalSeconds, float deltaTime)
        {
            if (avgIntervalSeconds <= 0f)
                return true; // edge case

            float probability = deltaTime / avgIntervalSeconds;
            return Random.value < probability;
        }

        public static bool RandomShouldOccur(float avgIntervalSeconds)
        {
            return RandomShouldOccur(avgIntervalSeconds, Time.fixedDeltaTime);
        }
    }
}
