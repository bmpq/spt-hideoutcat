using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace tarkin.hideoutcat.InverseKinematics
{
    public enum UpdateMode
    {
        Script,
        LateUpdate
    }

    public enum IKAlgorithm
    {
        FABRIK,
        CCD
    }

    [DefaultExecutionOrder(100)]
    [ExecuteAlways]
    public class BasicIK : MonoBehaviour
    {
        public UpdateMode updateMode = UpdateMode.Script;
        public IKAlgorithm algorithm = IKAlgorithm.FABRIK;
        public int numberOfJoints = 2;
        public Transform ikTarget;
        public Vector3 targetOffset;
        public int iterations = 7;
        public float tolerance = 0.01f;

        private Transform[] jointTransforms;
        private Vector3[] jointPositions;
        private Quaternion[] jointRotations;
        private float[] boneLength;
        private float jointChainLength;

        public Transform LastBone => jointTransforms[jointTransforms.Length - 1];
        public Transform[] Bones => jointTransforms;

        private JointConstraint[] constraints;
#if UNITY_EDITOR
        [Header("Editor Gizmos")]
        [Range(0.0f, 1.0f)]
        public float gizmoSize = 0.05f;
#endif
        void Awake()
        {
            InitializeJoints();
        }

        public void InitializeJoints()
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

        public void SetTarget(Transform target)
        {
            ikTarget = target;
        }

        private Vector3 GetTargetPosWithOffset()
        {
            return ikTarget.TransformPoint(targetOffset);
        }

        public float GetSolveResidual()
        {
            return Vector3.Distance(GetTargetPosWithOffset(), LastBone.position);
        }

        #region FABRIK Solver
        void Backward()
        {
            jointPositions[jointPositions.Length - 1] = GetTargetPosWithOffset();
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

                Quaternion parentParentRotation = (parentIndex > 0)
                    ? jointRotations[parentIndex - 1]
                    : (transform.parent != null ? transform.parent.rotation : Quaternion.identity);

                Quaternion desiredWorldRotation = Quaternion.LookRotation(direction,
                    parentTransform.parent != null ? parentTransform.parent.up : Vector3.up);

                Quaternion constrainedWorldRotation;

                if (constraint != null && constraint.enabled)
                {
                    Quaternion desiredLocalRotation = Quaternion.Inverse(parentParentRotation) * desiredWorldRotation;

                    Vector3 forwardInLocal = desiredLocalRotation * Vector3.forward;
                    float yawAngle = Vector3.SignedAngle(Vector3.forward,
                                                          Vector3.ProjectOnPlane(forwardInLocal, Vector3.up).normalized,
                                                          Vector3.up);

                    float clampedYawAngle = Mathf.Clamp(yawAngle, constraint.minLocalAngles.y, constraint.maxLocalAngles.y);
                    Quaternion yawRotation = Quaternion.AngleAxis(clampedYawAngle, Vector3.up);

                    Vector3 forwardAfterYaw = yawRotation * Vector3.forward;
                    float pitchAngle = Vector3.SignedAngle(forwardAfterYaw,
                                                            forwardInLocal,
                                                            Vector3.right);

                    float clampedPitchAngle = Mathf.Clamp(pitchAngle, constraint.minLocalAngles.x, constraint.maxLocalAngles.x);
                    Quaternion pitchRotation = Quaternion.AngleAxis(clampedPitchAngle, Vector3.right);

                    Quaternion yawPitchCombined = yawRotation * pitchRotation;
                    Vector3 upAfterYawPitch = yawPitchCombined * Vector3.up;
                    Vector3 upInLocal = desiredLocalRotation * Vector3.up;
                    float rollAngle = Vector3.SignedAngle(upAfterYawPitch,
                                                          upInLocal,
                                                          forwardInLocal);

                    float clampedRollAngle = Mathf.Clamp(rollAngle, constraint.minLocalAngles.z, constraint.maxLocalAngles.z);
                    Quaternion rollRotation = Quaternion.AngleAxis(clampedRollAngle, Vector3.forward);

                    Quaternion clampedLocalRotation = yawRotation * pitchRotation * rollRotation;
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

        private void SolveFABRIK()
        {
            for (int i = 0; i < jointTransforms.Length; i++)
            {
                jointPositions[i] = jointTransforms[i].position;
            }

            float targetDistance = Vector3.Distance(jointPositions[0], GetTargetPosWithOffset());

            if (targetDistance > jointChainLength)
            {
                Vector3 direction = (GetTargetPosWithOffset() - jointPositions[0]).normalized;
                for (int i = 1; i < jointPositions.Length; i++)
                {
                    jointPositions[i] = jointPositions[i - 1] + direction * boneLength[i - 1];
                }
            }
            else
            {
                for (int iter = 0; iter < iterations; iter++)
                {
                    if (Vector3.Distance(jointPositions.Last(), GetTargetPosWithOffset()) < tolerance)
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
        #endregion

        #region CCD Solver
        private void SolveCCD()
        {
            Transform endEffector = jointTransforms.Last();

            for (int iter = 0; iter < iterations; iter++)
            {
                if (Vector3.Distance(endEffector.position, GetTargetPosWithOffset()) < tolerance)
                    break;

                // Iterate from the end-effector's parent down to the root joint
                for (int i = jointTransforms.Length - 2; i >= 0; i--)
                {
                    Transform currentJoint = jointTransforms[i];
                    JointConstraint constraint = constraints[i];

                    Vector3 toEndEffector = (endEffector.position - currentJoint.position).normalized;
                    Vector3 toTarget = (GetTargetPosWithOffset() - currentJoint.position).normalized;

                    Quaternion deltaRotation = Quaternion.FromToRotation(toEndEffector, toTarget);
                    Quaternion potentialNewWorldRotation = deltaRotation * currentJoint.rotation;

                    if (constraint != null && constraint.enabled)
                    {
                        // To apply constraints, we work in the local space of the joint's parent
                        Quaternion parentRotation = (i > 0)
                            ? jointTransforms[i - 1].rotation
                            : (transform.parent != null ? transform.parent.rotation : Quaternion.identity);

                        Quaternion desiredLocalRotation = Quaternion.Inverse(parentRotation) * potentialNewWorldRotation;

                        // Decompose the desired rotation into "swing" and "twist"
                        // Twist is the rotation around the joint's primary axis (e.g., local 'up' for a yaw joint)
                        // Swing is the remaining rotation (e.g., pitch/roll)

                        // We will handle this by clamping each axis sequentially. Let's assume a Y-X-Z rotation order.
                        // This means we first determine Yaw (Y), then Pitch (X), then Roll (Z).

                        // 1. --- Clamp Yaw (Y-axis) ---
                        // We project a reference vector (like 'forward') onto the XZ plane and measure the angle.
                        Vector3 forwardInLocal = desiredLocalRotation * Vector3.forward;
                        float yawAngle = Vector3.SignedAngle(Vector3.forward,
                                                              Vector3.ProjectOnPlane(forwardInLocal, Vector3.up).normalized,
                                                              Vector3.up);

                        float clampedYawAngle = Mathf.Clamp(yawAngle, constraint.minLocalAngles.y, constraint.maxLocalAngles.y);
                        Quaternion yawRotation = Quaternion.AngleAxis(clampedYawAngle, Vector3.up);

                        // 2. --- Clamp Pitch (X-axis) ---
                        // Now we take the yaw-corrected reference and measure the pitch.
                        Vector3 forwardAfterYaw = yawRotation * Vector3.forward;
                        float pitchAngle = Vector3.SignedAngle(forwardAfterYaw,
                                                                forwardInLocal,
                                                                Vector3.right); // Use right-axis as the pivot for pitch

                        float clampedPitchAngle = Mathf.Clamp(pitchAngle, constraint.minLocalAngles.x, constraint.maxLocalAngles.x);
                        Quaternion pitchRotation = Quaternion.AngleAxis(clampedPitchAngle, Vector3.right);


                        // 3. --- Clamp Roll (Z-axis) ---
                        // Finally, we calculate the roll. We find the "up" vector after applying the desired yaw and pitch,
                        // and compare it to the "up" vector from the full desired rotation.
                        Quaternion yawPitchCombined = yawRotation * pitchRotation;
                        Vector3 upAfterYawPitch = yawPitchCombined * Vector3.up;
                        Vector3 upInLocal = desiredLocalRotation * Vector3.up;

                        float rollAngle = Vector3.SignedAngle(upAfterYawPitch,
                                                              upInLocal,
                                                              forwardInLocal); // Use the final forward vector as the axis of roll

                        float clampedRollAngle = Mathf.Clamp(rollAngle, constraint.minLocalAngles.z, constraint.maxLocalAngles.z);
                        Quaternion rollRotation = Quaternion.AngleAxis(clampedRollAngle, Vector3.forward);


                        // 4. --- Recombine and Apply ---
                        // We combine the clamped rotations in the correct order (Y, then X, then Z)
                        // Note: The order of multiplication is the reverse of the order of application.
                        Quaternion clampedLocalRotation = yawRotation * pitchRotation * rollRotation;

                        currentJoint.rotation = parentRotation * clampedLocalRotation;
                    }
                    else
                    {
                        currentJoint.rotation = potentialNewWorldRotation;
                    }
                }
            }

            endEffector.rotation = ikTarget.rotation;
        }
        #endregion

        public void SolveIK()
        {
            if (ikTarget == null || jointTransforms == null || jointTransforms.Length == 0) return;

            switch (algorithm)
            {
                case IKAlgorithm.FABRIK:
                    SolveFABRIK();
                    break;
                case IKAlgorithm.CCD:
                    SolveCCD();
                    break;
            }
        }

        private float ClampAngle(float angle, float min, float max)
        {
            while (angle > 180) angle -= 360;
            while (angle < -180) angle += 360;
            return Mathf.Clamp(angle, min, max);
        }

        void LateUpdate()
        {
            if (updateMode == UpdateMode.LateUpdate)
                SolveIK();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (Application.isPlaying) return;

            if (jointTransforms == null || jointTransforms.Length != numberOfJoints)
            {
                InitializeJoints();
            }
        }

        void OnDrawGizmos()
        {
            if (!enabled) return;

            if (jointTransforms != null && jointTransforms.Length > 0)
            {
                var current = jointTransforms[0];
                for (int i = 0; i < jointTransforms.Length - 1; i += 1)
                {
                    var child = jointTransforms[i + 1];
                    if (current == null || child == null) break;

                    float length = Vector3.Distance(current.position, child.position);
                    DrawWireCapsule(current.position + (child.position - current.position).normalized * length / 2,
                                     Quaternion.FromToRotation(Vector3.up, (child.position - current.position).normalized),
                                     gizmoSize, length, Color.cyan);
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