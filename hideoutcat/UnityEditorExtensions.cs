#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace tarkin.hideoutcat
{
    public static class UnityEditorExtensions
    {
        public static void DrawVolumetricCone(Vector3 apex, Vector3 direction, float angle, float length, Color color)
        {
            if (angle <= 0) return;
            if (angle >= 180)
            {
                Handles.color = color;
                Handles.DrawSolidDisc(apex, direction, length);
                return;
            }

            float coneAngleRad = angle * Mathf.Deg2Rad;
            float coneHeight = Mathf.Cos(coneAngleRad) * length;
            float coneRadius = Mathf.Sin(coneAngleRad) * length;
            Vector3 baseCenter = apex + direction * coneHeight;

            Quaternion rotation = Quaternion.LookRotation(direction);
            Vector3 up = rotation * Vector3.up;
            Vector3 right = rotation * Vector3.right;

            Handles.color = color;

            Handles.DrawSolidDisc(baseCenter, direction, coneRadius);

            const int segments = 32;
            Vector3 lastPoint = baseCenter + right * coneRadius;
            for (int i = 1; i <= segments; i++)
            {
                float rad = (i / (float)segments) * 2 * Mathf.PI;
                Vector3 currentPoint = baseCenter + (right * Mathf.Cos(rad) + up * Mathf.Sin(rad)) * coneRadius;
                Handles.DrawAAConvexPolygon(apex, lastPoint, currentPoint);
                lastPoint = currentPoint;
            }
        }

    }
}
#endif