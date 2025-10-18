using System.Collections.Generic;
using UnityEngine;

namespace tarkin
{
    public static class UnityExtensions
    {
        public static void SetPositionIndividualAxis(this Transform transform, float? x = null, float? y = null, float? z = null)
        {
            Vector3 pos = transform.position;
            if (x.HasValue) pos.x = x.Value;
            if (y.HasValue) pos.y = y.Value;
            if (z.HasValue) pos.z = z.Value;
            transform.position = pos;
        }

        public static float InverseLerpUnclamped(float a, float b, float value) => (value - a) / (b - a);
    }
}
