using UnityEngine;
using tarkin.hideoutcat.InverseKinematics;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace tarkin.hideoutcat
{
    [ExecuteAlways]
    public class BoneLookAt : MonoBehaviour
    {
        public Transform targetLookAt;

        [Space(10)]
        public Vector3 targetOffset = Vector3.zero;
        public Vector3 worldUpVector = Vector3.up;

        [Space(10)]
        public float smoothTime = 0.2f;
        public float resetSmoothTime = 0.5f; // separate smooth time for resetting when the target is null

        [Range(0, 1)]
        public float weight = 1f;

        private JointConstraint _jointConstraint;

        private Quaternion _currentRotation;
        private Quaternion _targetRotation;
        private Vector3 _currentAngularVelocity;

        private float _targetPresenceWeight = 0f;

        private BoneLookAt parent;
        private bool solvedThisFrame;

        void Awake()
        {
            TryGetComponent(out _jointConstraint);

            _currentRotation = transform.localRotation;

            parent = transform.parent?.GetComponent<BoneLookAt>();
        }

        void OnEnable()
        {
            _currentRotation = transform.localRotation;
            _targetPresenceWeight = (targetLookAt != null) ? 1f : 0f;
        }

        void Update()
        {
            solvedThisFrame = false;
        }

        // run after animator
        void LateUpdate()
        {
            if (!enabled) return;

            Solve();
        }

        public void Solve()
        {
            parent?.Solve();

            if (solvedThisFrame || !enabled)
                return;

            float targetWeight = (targetLookAt != null) ? 1f : 0f;
            _targetPresenceWeight = Mathf.Lerp(_targetPresenceWeight, targetWeight, Time.deltaTime * 5f);

            if (targetLookAt != null)
            {
                Vector3 targetPosition = targetLookAt.position + targetOffset;
                Vector3 direction = targetPosition - transform.position;

                if (direction.sqrMagnitude > 0.001f)
                {
                    Quaternion lookAtWorldRotation = Quaternion.LookRotation(direction, worldUpVector);

                    Quaternion targetLocalRotation = (transform.parent != null)
                        ? Quaternion.Inverse(transform.parent.rotation) * lookAtWorldRotation
                        : lookAtWorldRotation;

                    if (_jointConstraint != null)
                    {
                        targetLocalRotation = ApplyJointConstraint(targetLocalRotation);
                    }

                    _targetRotation = targetLocalRotation;
                }
            }
            else
            {
            }

            float currentSmoothTime = (targetLookAt != null) ? smoothTime : resetSmoothTime;
            _currentRotation = SmoothDampQuaternion(_currentRotation, _targetRotation, ref _currentAngularVelocity, currentSmoothTime);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, _currentRotation, weight * _targetPresenceWeight);

            solvedThisFrame = true;
        }

        private Quaternion ApplyJointConstraint(Quaternion localRotation)
        {
            Vector3 axis = _jointConstraint.twistAxis.normalized;

            JointConstraint.DecomposeSwingTwist(localRotation, axis, out Quaternion swing, out Quaternion twist);

            swing.ToAngleAxis(out float swingAngle, out Vector3 swingAxis);
            if (swingAngle > _jointConstraint.swingLimit)
            {
                swing = Quaternion.AngleAxis(_jointConstraint.swingLimit, swingAxis);
            }

            twist.ToAngleAxis(out float twistAngle, out Vector3 twistAxis_internal);

            if (twistAngle > 180f) twistAngle -= 360f;

            if (Vector3.Dot(twistAxis_internal, axis) < 0)
            {
                twistAngle *= -1f;
            }

            float clampedTwistAngle = Mathf.Clamp(twistAngle, _jointConstraint.twistLimitMin, _jointConstraint.twistLimitMax);
            twist = Quaternion.AngleAxis(clampedTwistAngle, axis);

            return swing * twist;
        }

        public static Quaternion SmoothDampQuaternion(Quaternion current, Quaternion target, ref Vector3 currentAngularVelocity, float smoothTime)
        {
            if (Quaternion.Dot(current, target) < 0)
            {
                target = new Quaternion(-target.x, -target.y, -target.z, -target.w);
            }

            Quaternion delta = target * Quaternion.Inverse(current);
            delta.ToAngleAxis(out float angle, out Vector3 axis);

            if (angle > 180f) angle -= 360f;

            float smoothedAngle = Mathf.SmoothDampAngle(0, angle, ref currentAngularVelocity.x, smoothTime);

            Quaternion smoothedDelta = Quaternion.AngleAxis(smoothedAngle, axis);

            return smoothedDelta * current;
        }
    }
}
