using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace tarkin.hideoutcat.InverseKinematics
{
    [DisallowMultipleComponent]
    public class JointConstraint : MonoBehaviour
    {
        public Vector3 minLocalAngles = new Vector3(-45f, -45f, -45f);
        public Vector3 maxLocalAngles = new Vector3(45f, 45f, 45f);

#if UNITY_EDITOR
        [Header("Gizmo Settings")]
        [Range(0.0f, 0.2f)]
        public float gizmoRadius = 0.05f;
        [Range(0.0f, 1.0f)]
        public float alpha = 0.25f;
        public bool alwaysShowGizmo = false;
        public bool showCurrentAngle = true;


        private void OnDrawGizmos()
        {
            if (alwaysShowGizmo)
            {
                DrawConstraintGizmos();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!alwaysShowGizmo)
            {
                DrawConstraintGizmos();
            }
        }

        private void DrawConstraintGizmos()
        {
            Matrix4x4 originalMatrix = Handles.matrix;
            // using parent matrix instead of own, otherwise the rotation itself would offset the range, resulting in incorrect visuals
            Handles.matrix = transform.parent.localToWorldMatrix;

            // -180 to 180 is simply better for visualization
            Vector3 currentAngles = transform.localEulerAngles;
            currentAngles.x = NormalizeAngle(currentAngles.x);
            currentAngles.y = NormalizeAngle(currentAngles.y);
            currentAngles.z = NormalizeAngle(currentAngles.z);

            Vector3 gizmoCenter = transform.parent.InverseTransformPoint(transform.position);

            DrawAxisGizmo(gizmoCenter, Vector3.right, Vector3.forward, minLocalAngles.x, maxLocalAngles.x, currentAngles.x, Handles.xAxisColor);
            DrawAxisGizmo(gizmoCenter, Vector3.up, Vector3.forward, minLocalAngles.y, maxLocalAngles.y, currentAngles.y, Handles.yAxisColor);
            DrawAxisGizmo(gizmoCenter, Vector3.forward, Vector3.up, minLocalAngles.z, maxLocalAngles.z, currentAngles.z, Handles.zAxisColor);

            Handles.matrix = originalMatrix;
        }

        private void DrawAxisGizmo(Vector3 center, Vector3 axis, Vector3 referenceVector, float minAngle, float maxAngle, float currentAngle, Color color)
        {
            Handles.color = AlphaMultiply(color, alpha);
            float angleRange = maxAngle - minAngle;
            Vector3 fromDirection = Quaternion.AngleAxis(minAngle, axis) * referenceVector;
            Handles.DrawSolidArc(center, axis, fromDirection, angleRange, gizmoRadius);

            if (showCurrentAngle)
            {
                Handles.color = color;
                Vector3 currentDirection = Quaternion.AngleAxis(currentAngle, axis) * referenceVector;
                Handles.DrawLine(center, center + currentDirection * gizmoRadius * 1.2f, 2f);
            }
        }

        private static float NormalizeAngle(float angle)
        {
            while (angle > 180)
                angle -= 360;
            while (angle < -180)
                angle += 360;
            return angle;
        }

        private static Color AlphaMultiply(Color color, float alpha)
        {
            color.a *= alpha;
            return color;
        }
#endif
    }
}