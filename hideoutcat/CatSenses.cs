using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace tarkin.hideoutcat
{
    public class CatSenses : MonoBehaviour
    {
        [SerializeField] private Transform visionOrigin;
        [SerializeField] private float visionFOV = 200f;
        [SerializeField] private float visionMaxDistance = 20f;
        [SerializeField] private LayerMask obstacleLayerMask = 1 << 12;

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool visualizeFOV;
        [SerializeField] private bool visualizeLOS;
#endif

        public bool HasLineOfSight(Transform target, out float distance)
        {
            if (target == null)
            {
                distance = visionMaxDistance;
                return false;
            }

            Vector3 directionToTarget = target.position - visionOrigin.position;
            distance = directionToTarget.magnitude;

            if (distance > visionMaxDistance)
                return false;

            if (Vector3.Angle(visionOrigin.forward, directionToTarget.normalized) > visionFOV / 2)
                return false;

            if (Physics.Raycast(visionOrigin.position, directionToTarget.normalized, distance - 0.1f, obstacleLayerMask))
            {
#if UNITY_EDITOR
                if (visualizeLOS)
                    Debug.DrawRay(visionOrigin.position, directionToTarget.normalized * distance, Color.red);
#endif
                return false;
            }

#if UNITY_EDITOR
            if (visualizeLOS)
                Debug.DrawLine(visionOrigin.position, directionToTarget.normalized * distance, Color.green);
#endif

            return true;
        }

        public Transform FindTargetInView(LayerMask targetLayerMask, string gameObjectTag)
        {
            Collider[] targetsInViewRadius = Physics.OverlapSphere(visionOrigin.position, visionMaxDistance, targetLayerMask);

            foreach (var targetCollider in targetsInViewRadius)
            {
                Transform targetTransform = targetCollider.transform;
                if (HasLineOfSight(targetTransform, out _))
                {
                    return targetTransform;
                }
            }

            return null;
        }


#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (visualizeFOV)
                UnityEditorExtensions.DrawVolumetricCone(visionOrigin.position, visionOrigin.forward, visionFOV / 2f, 0.3f, new Color(1f, 1f, 0.1f, 0.2f));
        }
#endif
    }
}
