using tarkin.hideoutcat.InverseKinematics;
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
            if (_constraint == null || _constraint.transform.parent == null)
                return;

            Transform transform = _constraint.transform;
            Transform parent = transform.parent;

            Matrix4x4 originalMatrix = Handles.matrix;
            Handles.matrix = parent.localToWorldMatrix;

            Vector3 center = parent.InverseTransformPoint(transform.position);

            EditorGUI.BeginChangeCheck();

            if (_constraint.handleRotation == Quaternion.identity)
                _constraint.handleRotation = Quaternion.LookRotation(_constraint.twistAxis, transform.up);

            Quaternion twistAxisRotation = _constraint.handleRotation;

            Quaternion newTwistAxisRotation = Handles.RotationHandle(twistAxisRotation, center);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_constraint, "Change Twist Axis");
                _constraint.handleRotation = newTwistAxisRotation;
                _constraint.twistAxis = (newTwistAxisRotation * Vector3.forward).normalized;
                EditorUtility.SetDirty(_constraint);
            }

            Vector3 twistAxis = _constraint.twistAxis.normalized;
            Vector3 fromDirection = Vector3.Cross(twistAxis, Vector3.up).normalized;
            if (fromDirection == Vector3.zero)
            {
                fromDirection = Vector3.Cross(twistAxis, Vector3.right).normalized;
            }

            EditorGUI.BeginChangeCheck();

            float newMin = _constraint.twistLimitMin;
            float newMax = _constraint.twistLimitMax;

            DrawAngleHandles(center, twistAxis, fromDirection, ref newMin, ref newMax);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_constraint, "Change Joint Twist Limits");
                _constraint.twistLimitMin = newMin;
                _constraint.twistLimitMax = newMax;
                EditorUtility.SetDirty(_constraint);
            }

            Handles.matrix = originalMatrix;
        }

        private void DrawAngleHandles(Vector3 center, Vector3 axis, Vector3 fromDirection, ref float minAngle, ref float maxAngle)
        {
            Handles.color = new Color(Handles.zAxisColor.r, Handles.zAxisColor.g, Handles.zAxisColor.b, 0.2f);
            Vector3 minDir = Quaternion.AngleAxis(minAngle, axis) * fromDirection;
            float sweepAngle = maxAngle - minAngle;
            Handles.DrawSolidArc(center, axis, minDir, sweepAngle, _constraint.gizmoRadius);

            Handles.color = Handles.zAxisColor;
            minAngle = AngleHandle(center, axis, fromDirection, minAngle, "min");
            maxAngle = Mathf.Max(minAngle, AngleHandle(center, axis, fromDirection, maxAngle, "max"));
        }

        private float AngleHandle(Vector3 center, Vector3 axis, Vector3 fromDirection, float angle, string label)
        {
            Vector3 handleDirection = Quaternion.AngleAxis(angle, axis) * fromDirection;
            Vector3 handlePosition = center + handleDirection * _constraint.gizmoRadius;

            float handleSize = HandleUtility.GetHandleSize(handlePosition) * 0.1f;

            Vector3 newHandlePosition = Handles.FreeMoveHandle(handlePosition, handleSize, Vector3.zero, Handles.SphereHandleCap);

            if (newHandlePosition != handlePosition)
            {
                Vector3 projectedVector = newHandlePosition - center;
                Vector3 flattenedVector = Vector3.ProjectOnPlane(projectedVector, axis);

                float deltaAngle = Vector3.SignedAngle(handleDirection, flattenedVector.normalized, axis);

                return angle + deltaAngle;
            }

            return angle;
        }
    }
}