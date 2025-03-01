using UnityEngine;

namespace tarkin
{
    public static class SeeSharpExtensions
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        /// <summary>
        /// Unclamped reverse interpolation
        /// </summary>
        public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            // check for division by zero
            if (fromMax - fromMin == 0)
            {
                return (toMin + toMax) / 2f;
            }

            return toMin + ((value - fromMin) / (fromMax - fromMin)) * (toMax - toMin);
        }

        public static float RemapClamped(this float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            if (value < fromMin)
                value = fromMin;
            else if (value > fromMax)
                value = fromMax;

            return Remap(value, fromMin, fromMax, toMin, toMax);
        }
    }
}