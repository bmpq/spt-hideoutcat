using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace tarkin.hideoutcat.BasicIK
{
    [DefaultExecutionOrder(100)]
    [ExecuteAlways]
    public class BasicIK : MonoBehaviour
    {
        public int numberOfJoints = 2;
        public Transform ikTarget;
        public int iterations = 7;
        public float tolerance = 0.01f;

        private Transform[] jointTransforms;
        private Vector3[] jointPositions;
        private Quaternion[] jointRotations;
        private float[] boneLength;
        private float jointChainLength;

        private JointConstraint[] constraints;
#if UNITY_EDITOR
        [Range(0.0f, 1.0f)]
        public float gizmoSize = 0.05f;
#endif

        public void Awake()
        {
            jointChainLength = 0;
            jointTransforms = new Transform[numberOfJoints];
            jointPositions = new Vector3[numberOfJoints];
            jointRotations = new Quaternion[numberOfJoints];
            boneLength = new float[numberOfJoints - 1];
            constraints = new JointConstraint[numberOfJoints];

            var current = transform;
            for (var i = 0; i < jointTransforms.Length; i++, current = current.childCount > 0 ? current.GetChild(0) : null)
            {
                if (current == null)
                {
                    Debug.LogError("EasyIK Error: Not enough children in the chain to match numberOfJoints.", this);
                    enabled = false;
                    return;
                }

                jointTransforms[i] = current;
                constraints[i] = current.GetComponent<JointConstraint>();

                if (i < jointTransforms.Length - 1)
                {
                    var child = current.GetChild(0);
                    boneLength[i] = Vector3.Distance(current.position, child.position);
                    jointChainLength += boneLength[i];
                }
            }
        }

        void Backward()
        {
            jointPositions[jointPositions.Length - 1] = ikTarget.position;
            for (int i = jointPositions.Length - 2; i >= 0; i--)
            {
                Vector3 direction = (jointPositions[i] - jointPositions[i + 1]).normalized;
                jointPositions[i] = jointPositions[i + 1] + direction * boneLength[i];
            }
        }

        void Forward()
        {
            jointPositions[0] = jointTransforms[0].position;
            jointRotations[0] = jointTransforms[0].rotation;

            for (int i = 1; i < jointPositions.Length; i++)
            {
                int parentIndex = i - 1;
                Transform parentTransform = jointTransforms[parentIndex];
                JointConstraint constraint = constraints[parentIndex];

                Vector3 direction = (jointPositions[i] - jointPositions[parentIndex]).normalized;

                Quaternion parentParentRotation = (parentIndex > 0) ? jointRotations[parentIndex - 1] : transform.parent != null ? transform.parent.rotation : Quaternion.identity;

                Quaternion constrainedWorldRotation;

                Quaternion desiredWorldRotation = Quaternion.LookRotation(direction, parentTransform.parent != null ? parentTransform.parent.up : Vector3.up);

                if (constraint != null)
                {
                    Quaternion desiredLocalRotation = Quaternion.Inverse(parentParentRotation) * desiredWorldRotation;
                    Vector3 localAngles = desiredLocalRotation.eulerAngles;
                    Vector3 clampedAngles = new Vector3(
                        ClampAngle(localAngles.x, constraint.minLocalAngles.x, constraint.maxLocalAngles.x),
                        ClampAngle(localAngles.y, constraint.minLocalAngles.y, constraint.maxLocalAngles.y),
                        ClampAngle(localAngles.z, constraint.minLocalAngles.z, constraint.maxLocalAngles.z)
                    );

                    Quaternion clampedLocalRotation = Quaternion.Euler(clampedAngles);
                    constrainedWorldRotation = parentParentRotation * clampedLocalRotation;
                }
                else
                {
                    constrainedWorldRotation = desiredWorldRotation;
                }

                jointRotations[parentIndex] = constrainedWorldRotation;

                direction = constrainedWorldRotation * Vector3.forward;
                jointPositions[i] = jointPositions[parentIndex] + direction * boneLength[parentIndex];
            }
        }

        private void SolveIK()
        {
            if (ikTarget == null) return;

            for (int i = 0; i < jointTransforms.Length; i++)
            {
                jointPositions[i] = jointTransforms[i].position;
            }

            float targetDistance = Vector3.Distance(jointPositions[0], ikTarget.position);

            if (targetDistance > jointChainLength)
            {
                Vector3 direction = (ikTarget.position - jointPositions[0]).normalized;
                for (int i = 1; i < jointPositions.Length; i++)
                {
                    jointPositions[i] = jointPositions[i - 1] + direction * boneLength[i - 1];
                }
            }
            else
            {
                for (int iter = 0; iter < iterations; iter++)
                {
                    if (Vector3.Distance(jointPositions.Last(), ikTarget.position) < tolerance)
                        break;

                    Backward();
                    Forward();
                }
            }

            for (int i = 0; i < jointTransforms.Length - 1; i++)
            {
                jointTransforms[i].rotation = jointRotations[i];
            }

            jointTransforms.Last().rotation = ikTarget.rotation;
        }


        private float ClampAngle(float angle, float min, float max)
        {
            while (angle > 180) angle -= 360;
            while (angle < -180) angle += 360;
            return Mathf.Clamp(angle, min, max);
        }

        void LateUpdate()
        {
            SolveIK();
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (jointTransforms != null && jointTransforms.Length > 0)
            {
                var current = transform;
                for (int i = 0; i < numberOfJoints - 1; i += 1)
                {
                    if (current == null || current.childCount == 0) break;
                    var child = current.GetChild(0);
                    var length = Vector3.Distance(current.position, child.position);
                    DrawWireCapsule(current.position + (child.position - current.position).normalized * length / 2, Quaternion.FromToRotation(Vector3.up, (child.position - current.position).normalized), gizmoSize, length, Color.cyan);
                    current = child;
                }
            }
        }

        public static void DrawWireCapsule(Vector3 _pos, Quaternion _rot, float _radius, float _height, Color _color = default(Color))
        {
            Handles.color = _color;
            Matrix4x4 angleMatrix = Matrix4x4.TRS(_pos, _rot, Handles.matrix.lossyScale);
            using (new Handles.DrawingScope(angleMatrix))
            {
                var pointOffset = (_height - (_radius * 2)) / 2;

                Handles.DrawWireArc(Vector3.up * pointOffset, Vector3.left, Vector3.back, -180, _radius);
                Handles.DrawLine(new Vector3(0, pointOffset, -_radius), new Vector3(0, -pointOffset, -_radius));
                Handles.DrawLine(new Vector3(0, pointOffset, _radius), new Vector3(0, -pointOffset, _radius));
                Handles.DrawWireArc(Vector3.down * pointOffset, Vector3.left, Vector3.back, 180, _radius);

                Handles.DrawWireArc(Vector3.up * pointOffset, Vector3.back, Vector3.left, 180, _radius);
                Handles.DrawLine(new Vector3(-_radius, pointOffset, 0), new Vector3(-_radius, -pointOffset, 0));
                Handles.DrawLine(new Vector3(_radius, pointOffset, 0), new Vector3(_radius, -pointOffset, 0));
                Handles.DrawWireArc(Vector3.down * pointOffset, Vector3.back, Vector3.left, -180, _radius);

                Handles.DrawWireDisc(Vector3.up * pointOffset, Vector3.up, _radius);
                Handles.DrawWireDisc(Vector3.down * pointOffset, Vector3.up, _radius);
            }
        }
#endif
    }
}