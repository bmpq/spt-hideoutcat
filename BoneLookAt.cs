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

        // LateUpdate to override Animator
        void LateUpdate()
        {
            if (bone == null || targetLookAt == null)
            {
                return;
            }

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

            bone.localRotation = Quaternion.Slerp(bone.localRotation, targetLocalRotation, weight);
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
