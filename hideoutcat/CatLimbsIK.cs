using UnityEngine;
using tarkin.hideoutcat.InverseKinematics;

namespace tarkin.hideoutcat
{
    internal class CatLimbsIK : MonoBehaviour
    {
        [SerializeField] private LayerMask raycastMask;

        [SerializeField] private BasicIK[] limbs;

        [Range(0f, 1f)]
        [Tooltip("ik influence over animator")]
        [SerializeField] private float factor;

        private Transform[] ikTargets;

        void Awake()
        {
            ikTargets = new Transform[limbs.Length];

            for (int i = 0; i < ikTargets.Length; i++)
            {
                ikTargets[i] = new GameObject("IKTarget").transform;
                limbs[i].SetTarget(ikTargets[i]);
                limbs[i].updateMode = UpdateMode.Script;
            }
        }

        void LateUpdate()
        {
            for (int i = 0; i < limbs.Length; i++)
            {
                Transform a = limbs[i].transform;
                Transform b = limbs[i].LastBone;
                Vector3 dir = b.position - a.position;
                float dist = Vector3.Distance(a.position, b.position);

                if (Physics.Raycast(a.position, dir, out RaycastHit hit, dist, raycastMask))
                {
                    ikTargets[i].position = hit.point;
                    ikTargets[i].rotation = transform.rotation;

                    Vector3[] animatorPos = new Vector3[limbs[i].Bones.Length];
                    Quaternion[] animatorRot = new Quaternion[limbs[i].Bones.Length];
                    for (int j = 0; j < limbs[i].Bones.Length; j++)
                    {
                        animatorPos[j] = limbs[i].Bones[j].localPosition;
                        animatorRot[j] = limbs[i].Bones[j].localRotation;
                    }

                    limbs[i].SolveIK(); // modifies transforms

                    for (int j = 0; j < limbs[i].Bones.Length; j++)
                    {
                        limbs[i].Bones[j].localPosition = Vector3.Lerp(animatorPos[j], limbs[i].Bones[j].localPosition, factor);
                        limbs[i].Bones[j].localRotation = Quaternion.Lerp(animatorRot[j], limbs[i].Bones[j].localRotation, factor);
                    }
                }
            }
        }

        void OnDestroy()
        {
            for (int i = 0; i < limbs.Length; i++)
            {
                Destroy(ikTargets[i]?.gameObject);
            }
        }
    }
}
