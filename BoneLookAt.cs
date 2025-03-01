using UnityEngine;

namespace hideoutcat
{
    public class BoneLookAt : MonoBehaviour
    {
        public Transform targetLookAt;
        public Transform bone;
        public Vector3 targetOffset = Vector3.zero;
        public float weight = 1f;
        public Vector3 customUpVector = Vector3.zero; // if zero use local
        public bool useAngleLimits = false;
        public Vector3 maxAngleLimits = new Vector3(45f, 45f, 45f);
        public Vector3 minAngleLimits = new Vector3(-45f, -45f, -45f);

        public float smoothTime = 0.2f;
        public float resetSmoothTime = 0.5f; // separate smooth time for resetting when the target is null

        public Vector3 rotationOffsetEuler
        {
            get
            {
                return _rotationOffsetEuler;
            }
            set
            {
                _rotationOffsetEuler = value;
                _rotationOffset = Quaternion.Euler(_rotationOffsetEuler);
            }
        }
        private Vector3 _rotationOffsetEuler = Vector3.zero;
        private Quaternion _rotationOffset = Quaternion.identity;

        private Quaternion _currentRotation;
        private Quaternion _targetRotation;
        private Vector3 _currentAngularVelocity = Vector3.zero;
        private Quaternion _initialLocalRotation;

        void Start()
        {
            if (bone != null)
            {
                _currentRotation = bone.localRotation;
                _initialLocalRotation = bone.localRotation;
            }
            _targetRotation = _currentRotation;
        }

        // running LateUpdate() to override Animator
        void LateUpdate()
        {
            if (bone == null)
            {
                return;
            }

            if (targetLookAt != null)
            {
                // Calculate the target rotation as before
                Vector3 finalTargetPosition = targetLookAt.position + targetOffset;
                Vector3 upVector = (customUpVector == Vector3.zero) ? bone.up : customUpVector;
                Quaternion lookAtRotation = Quaternion.LookRotation(finalTargetPosition - bone.position, upVector);

                Quaternion targetLocalRotation = Quaternion.identity;
                if (bone.parent != null)
                {
                    targetLocalRotation = Quaternion.Inverse(bone.parent.rotation) * lookAtRotation;
                }
                else
                {
                    targetLocalRotation = lookAtRotation;
                }

                targetLocalRotation *= _rotationOffset;

                if (useAngleLimits)
                {
                    targetLocalRotation = ClampRotation(targetLocalRotation);
                }

                _targetRotation = targetLocalRotation;
            }
            else
            {
                _targetRotation = _initialLocalRotation;
            }

            float currentSmoothTime = (targetLookAt != null) ? smoothTime : resetSmoothTime;

            _currentRotation = SmoothDampQuaternion(_currentRotation, _targetRotation, ref _currentAngularVelocity, currentSmoothTime);
            bone.localRotation = Quaternion.Slerp(bone.localRotation, _currentRotation, weight);

        }

        public static Quaternion SmoothDampQuaternion(Quaternion current, Quaternion target, ref Vector3 currentAngularVelocity, float smoothTime)
        {
            // idk bro, I stole this, seems to work fine
            float dot = Quaternion.Dot(current, target);
            float sign = dot > 0f ? 1f : -1f;
            target.x *= sign;
            target.y *= sign;
            target.z *= sign;
            target.w *= sign;

            Vector3 eulerAngles = new Vector3(
               Mathf.SmoothDampAngle(current.eulerAngles.x, target.eulerAngles.x, ref currentAngularVelocity.x, smoothTime),
               Mathf.SmoothDampAngle(current.eulerAngles.y, target.eulerAngles.y, ref currentAngularVelocity.y, smoothTime),
               Mathf.SmoothDampAngle(current.eulerAngles.z, target.eulerAngles.z, ref currentAngularVelocity.z, smoothTime)
           );

            return Quaternion.Euler(eulerAngles);
        }

        private Quaternion ClampRotation(Quaternion targetRotation)
        {
            Vector3 eulerAngles = targetRotation.eulerAngles;

            eulerAngles.x = NormalizeAngle(eulerAngles.x);
            eulerAngles.y = NormalizeAngle(eulerAngles.y);
            eulerAngles.z = NormalizeAngle(eulerAngles.z);

            eulerAngles.x = Mathf.Clamp(eulerAngles.x, minAngleLimits.x, maxAngleLimits.x);
            eulerAngles.y = Mathf.Clamp(eulerAngles.y, minAngleLimits.y, maxAngleLimits.y);
            eulerAngles.z = Mathf.Clamp(eulerAngles.z, minAngleLimits.z, maxAngleLimits.z);

            return Quaternion.Euler(eulerAngles);
        }

        private float NormalizeAngle(float angle)
        {
            while (angle > 180) angle -= 360;
            while (angle < -180) angle += 360;
            return angle;
        }
    }
}
