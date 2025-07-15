using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace tarkin.hideoutcat.BasicIK
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
        public float alpha = 0.5f;
        public bool alwaysShowGizmo = false;

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

            Handles.color = AlphaMultiply(Handles.xAxisColor, alpha);
            float angleX = maxLocalAngles.x - minLocalAngles.x;
            Vector3 fromDirectionX = Quaternion.AngleAxis(minLocalAngles.x, Vector3.right) * Vector3.forward;
            Handles.DrawSolidArc(transform.parent.InverseTransformPoint(transform.position), Vector3.right, fromDirectionX, angleX, gizmoRadius);

            Handles.color = AlphaMultiply(Handles.yAxisColor, alpha);
            float angleY = maxLocalAngles.y - minLocalAngles.y;
            Vector3 fromDirectionY = Quaternion.AngleAxis(minLocalAngles.y, Vector3.up) * Vector3.forward;
            Handles.DrawSolidArc(transform.parent.InverseTransformPoint(transform.position), Vector3.up, fromDirectionY, angleY, gizmoRadius);

            Handles.color = AlphaMultiply(Handles.zAxisColor, alpha);
            float angleZ = maxLocalAngles.z - minLocalAngles.z;
            Vector3 fromDirectionZ = Quaternion.AngleAxis(minLocalAngles.z, Vector3.forward) * Vector3.up;
            Handles.DrawSolidArc(transform.parent.InverseTransformPoint(transform.position), Vector3.forward, fromDirectionZ, angleZ, gizmoRadius);

            Handles.matrix = originalMatrix;
        }

        private static Color AlphaMultiply(Color color, float alpha)
        {
            color.a *= alpha;
            return color;
        }
#endif
    }
}