using tarkin.hideoutcat.BasicIK;
using UnityEditor;
using UnityEngine;

namespace tarkin.hideoutcat.editor
{
    [CustomEditor(typeof(JointConstraint))]
    public class JointConstraintEditor : Editor
    {
        private JointConstraint _constraint;

        private void OnEnable()
        {
            _constraint = target as JointConstraint;
        }

        private void OnSceneGUI()
        {
            if (_constraint == null)
                return;

            Transform transform = _constraint.transform;
            Transform parent = transform.parent;

            Undo.RecordObject(_constraint, "Change Joint Constraint Angles");

            Matrix4x4 originalMatrix = Handles.matrix;
            Handles.matrix = parent.localToWorldMatrix;

            Vector3 localPos = transform.localPosition;

            EditorGUI.BeginChangeCheck();

            Vector3 newMin = _constraint.minLocalAngles;
            Vector3 newMax = _constraint.maxLocalAngles;

            DrawAngleHandles(transform.parent.InverseTransformPoint(transform.position), Vector3.right, Vector3.forward, Handles.xAxisColor, ref newMin.x, ref newMax.x);
            DrawAngleHandles(transform.parent.InverseTransformPoint(transform.position), Vector3.up, Vector3.forward, Handles.yAxisColor, ref newMin.y, ref newMax.y);
            DrawAngleHandles(transform.parent.InverseTransformPoint(transform.position), Vector3.forward, Vector3.up, Handles.zAxisColor, ref newMin.z, ref newMax.z);

            // a handle was moved
            if (EditorGUI.EndChangeCheck())
            {
                _constraint.minLocalAngles = newMin;
                _constraint.maxLocalAngles = newMax;
                EditorUtility.SetDirty(_constraint);
            }

            Handles.matrix = originalMatrix;
        }

        private void DrawAngleHandles(Vector3 center, Vector3 axis, Vector3 fromDirection, Color color, ref float minAngle, ref float maxAngle)
        {
            minAngle = AngleHandle(center, axis, fromDirection, minAngle, color, "min");
            maxAngle = AngleHandle(center, axis, fromDirection, maxAngle, color, "max");
        }

        private float AngleHandle(Vector3 center, Vector3 axis, Vector3 fromDirection, float angle, Color color, string label)
        {
            Vector3 handleDirection = Quaternion.AngleAxis(angle, axis) * fromDirection;
            Vector3 handlePosition = center + handleDirection * _constraint.gizmoRadius;

            float handleSize = HandleUtility.GetHandleSize(handlePosition) * 0.1f;

            Handles.color = color;
            Vector3 newHandlePosition = Handles.FreeMoveHandle(handlePosition, handleSize, Vector3.zero, Handles.SphereHandleCap);

            // handle was moved
            if (newHandlePosition != handlePosition)
            {
                Vector3 projectedVector = newHandlePosition - center;
                Vector3 flattenedVector = Vector3.ProjectOnPlane(projectedVector, axis);

                return Vector3.SignedAngle(fromDirection, flattenedVector, axis);
            }

            return angle;
        }
    }
}