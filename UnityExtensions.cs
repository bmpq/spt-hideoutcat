using UnityEngine;

namespace tarkin
{
    public class UnityExtensions
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
    }
}
