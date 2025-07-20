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

        // lateupdate to read and write after animator
        void LateUpdate()
        {
            int hitsFound = 0;

            float smallestAnimatorHitDelta = float.MaxValue;

            for (int i = 0; i < limbs.Length; i++)
            {
                Vector3 a = limbs[i].transform.position;
                Vector3 b = limbs[i].LastBone.position - limbs[i].targetOffset;
                Vector3 dir = Vector3.Normalize(b - a);
                float dist = Vector3.Distance(a, b);

                IMGUIDebugDraw.OnGUIDispatcher.Instance.Enqueue(() => IMGUIDebugDraw.Draw.DrawEdge(Camera.main, a, b, 1f, Color.green));

                if (Physics.Raycast(a, dir, out RaycastHit hit, dist, raycastMask))
                {
                    hitsFound++;
                    smallestAnimatorHitDelta = Mathf.Min(smallestAnimatorHitDelta, dist - hit.distance);

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

            float yDelta = 0f;

            bool falling = (hitsFound < 2);

            if (falling)
            {
                fallingSpeed += Time.deltaTime / 2f;
                yDelta = Physics.gravity.y * fallingSpeed * Time.deltaTime;
            }
            else
            {
                fallingSpeed = 0f;
                
                yDelta = Time.deltaTime * smallestAnimatorHitDelta * 5f;
            }

            transform.SetPositionIndividualAxis(y: transform.position.y + yDelta);
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
