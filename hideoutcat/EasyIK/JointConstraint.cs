using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace tarkin.hideoutcat.InverseKinematics
{
    [DisallowMultipleComponent]
    public class JointConstraint : MonoBehaviour
    {
        public Vector3 twistAxis = Vector3.forward;

        [Range(0, 180)]
        public float swingLimit = 45f;

        public float twistLimitMin = -90f;
        public float twistLimitMax = 90f; 
        
        public static void DecomposeSwingTwist(Quaternion rotation, Vector3 twistAxis, out Quaternion swing, out Quaternion twist)
        {
            Vector3 rotatedTwistAxis = rotation * twistAxis;
            swing = Quaternion.FromToRotation(twistAxis, rotatedTwistAxis);
            twist = Quaternion.Inverse(swing) * rotation;
        }

#if UNITY_EDITOR
        public Quaternion handleRotation { get; set; } = Quaternion.identity;

        [Header("Gizmo Settings")]
        [Range(0.0f, 0.2f)]
        public float gizmoRadius = 0.05f;
        [Range(0.0f, 1.0f)]
        public float alpha = 0.25f;
        public bool alwaysShowGizmo = false;
        public bool showCurrentRotation = true;
        
        void Start() 
        {
        }

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
            if (transform.parent == null) return;

            Matrix4x4 originalMatrix = Handles.matrix;
            Handles.matrix = transform.parent.localToWorldMatrix;
            Vector3 gizmoCenter = transform.parent.InverseTransformPoint(transform.position);

            UnityEditorExtensions.DrawVolumetricCone(
                gizmoCenter,
                twistAxis.normalized,
                swingLimit,
                gizmoRadius,
                AlphaMultiply(Color.yellow, alpha)
            );

            Handles.color = AlphaMultiply(Handles.zAxisColor, alpha);
            Vector3 twistReference = Vector3.Cross(twistAxis, Vector3.up).normalized;
            if (twistReference == Vector3.zero) twistReference = Vector3.right;

            float twistRange = twistLimitMax - twistLimitMin;
            Vector3 fromDirection = Quaternion.AngleAxis(twistLimitMin, twistAxis) * twistReference;
            Handles.DrawSolidArc(gizmoCenter, twistAxis, fromDirection, twistRange, gizmoRadius);

            if (showCurrentRotation)
            {
                Handles.color = Color.white;
                Vector3 currentBoneDirection = transform.localRotation * twistAxis.normalized;
                Handles.DrawLine(gizmoCenter, gizmoCenter + currentBoneDirection * gizmoRadius, 3f);

                Quaternion currentLocalRotation = transform.localRotation;
                Vector3 normalizedTwistAxis = twistAxis.normalized;
                DecomposeSwingTwist(currentLocalRotation, normalizedTwistAxis, out _, out Quaternion currentTwist);
                currentTwist.ToAngleAxis(out float currentTwistAngle, out Vector3 axis);

                if (Vector3.Dot(axis, normalizedTwistAxis) < 0)
                {
                    currentTwistAngle *= -1;
                }

                Vector3 currentTwistDirection = Quaternion.AngleAxis(currentTwistAngle, normalizedTwistAxis) * twistReference;
                Handles.color = Handles.zAxisColor;
                Handles.DrawLine(gizmoCenter, gizmoCenter + currentTwistDirection * (gizmoRadius * 1.2f), 2f);
            }

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