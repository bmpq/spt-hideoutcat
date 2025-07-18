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

        [SerializeField] private float residualTolerance = 0.1f;

        private Transform[] ikTargets;

        private float fallingSpeed;

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
            float furthestHitDistance = -1f;
            float secondFurthestHitDistance = -1f;

            float floorDelta = 0;
            float furthestFloorDelta = 0;
            int hitsFound = 0;

            for (int i = 0; i < limbs.Length; i++)
            {
                Transform a = limbs[i].transform;
                Vector3 b = limbs[i].LastBone.position - limbs[i].targetOffset;
                Vector3 dir = b - a.position;
                float dist = Vector3.Distance(a.position, b);

                if (Physics.Raycast(a.position, dir, out RaycastHit hit, dist * 2f, raycastMask))
                {
                    hitsFound++;
                    float currentHitDistance = hit.distance;
                    float currentFloorDelta = hit.point.y - b.y;

                    if (currentHitDistance > furthestHitDistance)
                    {
                        secondFurthestHitDistance = furthestHitDistance;
                        floorDelta = furthestFloorDelta;

                        furthestHitDistance = currentHitDistance;
                        furthestFloorDelta = currentFloorDelta;
                    }
                    else if (currentHitDistance > secondFurthestHitDistance)
                    {
                        secondFurthestHitDistance = currentHitDistance;
                        floorDelta = currentFloorDelta;
                    }

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

                    float _factor = factor;
                    float residual = limbs[i].GetSolveResidual();
                    if (residual > residualTolerance)
                        _factor = 0f;

                    for (int j = 0; j < limbs[i].Bones.Length; j++)
                    {
                        limbs[i].Bones[j].localPosition = Vector3.Lerp(animatorPos[j], limbs[i].Bones[j].localPosition, _factor);
                        limbs[i].Bones[j].localRotation = Quaternion.Lerp(animatorRot[j], limbs[i].Bones[j].localRotation, _factor);
                    }
                }
            }

            if (hitsFound == 1)
            {
                floorDelta = furthestFloorDelta;
            }

            const float floorCorrectionStrength = 5f;

            if (floorDelta < 0)
            {
                fallingSpeed += Time.deltaTime / 2f;
                floorDelta = Physics.gravity.y / 5f * fallingSpeed;
            }
            else
                fallingSpeed = 0f;
            transform.SetPositionIndividualAxis(y: transform.position.y + floorDelta * floorCorrectionStrength * Time.deltaTime);
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
