using UnityEngine;
using tarkin.hideoutcat;

namespace tarkin.hideoutcat
{
    public class CatAttackHandlerPrototype : MonoBehaviour
    {
        private Animator animator;
        private HideoutCat cat;
        private CatLookAt lookAt;

        [SerializeField] private Transform visionOrigin;
        [SerializeField] private float visionFOV = 200f;
        [SerializeField] private float visionMaxDistance = 20f;
        [SerializeField] private LayerMask layerMask = 1 << 12;

#if UNITY_EDITOR
        [SerializeField] private bool visualizeFOV;
#endif

        private Transform target;

        void Awake()
        {
            animator = GetComponent<Animator>();
            cat = GetComponent<HideoutCat>();
            lookAt = GetComponent<CatLookAt>();
        }

        void Update()
        {
            if (target != null)
            {
                if (LineOfSight())
                {
                    Debug.DrawLine(visionOrigin.position, target.position, Color.green);
                    lookAt.SetLookTarget(target);
                }
                else
                {
                    lookAt.SetLookTarget(null);
                }
            }
        }

        bool LineOfSight()
        {
            Vector3 directionToTarget = (target.position - visionOrigin.position);
            float distanceToTarget = directionToTarget.magnitude;

            if (distanceToTarget > visionMaxDistance)
                return false;

            if (Vector3.Angle(visionOrigin.forward, directionToTarget.normalized) > visionFOV / 2)
                return false;

            if (Physics.Raycast(visionOrigin.position, directionToTarget.normalized, out RaycastHit hit, distanceToTarget, layerMask))
            {
                Debug.DrawLine(visionOrigin.position, hit.point, Color.red);

                return false;
            }

            return true;
        }

        public void SetTarget(Transform _target)
        {
            this.target = _target;
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
