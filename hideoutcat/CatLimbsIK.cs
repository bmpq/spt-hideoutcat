using UnityEngine;
using tarkin.hideoutcat.InverseKinematics;
using System.Collections.Generic;

namespace tarkin.hideoutcat
{
    internal class CatLimbsIK : MonoBehaviour
    {
        [SerializeField] private LayerMask raycastMask;

        [SerializeField] private BasicIK[] limbs;

        [Range(0f, 1f)]
        [Tooltip("ik influence over animator")]
        [SerializeField] private float factor = 1f;

        [Range(0f, 0.2f)]
        [SerializeField] private float ikRaycastOvershoot = 0.06f;
        
        [SerializeField] private float heightCorrectionSpeed = 15f;
        [SerializeField] private float fallingSpeed = 0.2f;

        [Header("body tilt")]
        [SerializeField] private Transform tiltBone;
        private Quaternion currentTilt;
        [Range(0f, 1f)]
        public float tiltFactor = 1f;

        [SerializeField] private float tiltCorrectionSpeed = 1f;
        [SerializeField] private float steepSlopeAngle = 20f;
        
        [SerializeField] private float groundCheckDistance = 1f;

        private Transform[] ikTargets;

        private Dictionary<Transform, (Vector3, Quaternion)> prevFrame;

        private float currentFallingSpeed;

        private Vector3 currentUp = Vector3.up;

        public bool yControl { get; set; }

        void Awake()
        {
            ikTargets = new Transform[limbs.Length];

            for (int i = 0; i < ikTargets.Length; i++)
            {
                ikTargets[i] = new GameObject("IKTarget").transform;
                limbs[i].SetTarget(ikTargets[i]);
                limbs[i].updateMode = UpdateMode.Script;
            }

            prevFrame = new Dictionary<Transform, (Vector3, Quaternion)>();
            StoreBoneCurrentData();
        }

        // lateupdate to read and write after animator
        void LateUpdate()
        {
            float smallestAnimatorHitDelta = float.MaxValue;

            float slopeAngle = Vector3.Angle(currentUp, Vector3.up);

            int hitsFound = 0;
            for (int i = 0; i < limbs.Length; i++)
            {
                Vector3 a = limbs[i].transform.position;
                Vector3 b = limbs[i].LastBone.position - limbs[i].targetOffset;
                float animatorDrivenLimbLength = Vector3.Distance(a, b);

                // if the cat is on a slope, the raycast direction is straight down, instead of an animator-driven limb leaf
                Vector3 dir = Vector3.Lerp((b - a).normalized, -transform.up, Mathf.Clamp01(Mathf.InverseLerp(0, steepSlopeAngle, slopeAngle))).normalized;

                if (Physics.Raycast(a, dir, out RaycastHit hit, animatorDrivenLimbLength + ikRaycastOvershoot, raycastMask))
                {
                    hitsFound++;

                    smallestAnimatorHitDelta = Mathf.Min(smallestAnimatorHitDelta, animatorDrivenLimbLength - hit.distance);

                    Debug.DrawLine(a, a + dir * (animatorDrivenLimbLength + ikRaycastOvershoot), Color.red, default, false);

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

                    float residual = limbs[i].GetSolveResidual();
                }
                else
                {
                    Debug.DrawLine(a, a + dir * (animatorDrivenLimbLength + ikRaycastOvershoot), Color.white, default, false);
                }

                for (int j = 0; j < limbs[i].Bones.Length; j++)
                {
                    limbs[i].Bones[j].localPosition = Vector3.Lerp(
                        prevFrame[limbs[i].Bones[j].transform].Item1, limbs[i].Bones[j].localPosition, 0.5f);
                    limbs[i].Bones[j].localRotation = Quaternion.Slerp(
                        prevFrame[limbs[i].Bones[j].transform].Item2, limbs[i].Bones[j].localRotation, 0.5f);
                }
            }

            float yDelta = 0f;

            bool falling = (hitsFound < 2);

            if (slopeAngle > steepSlopeAngle)
                falling = hitsFound < 4;

            if (yControl && falling)
            {
                currentFallingSpeed += Time.deltaTime * fallingSpeed;
                yDelta = Physics.gravity.y * currentFallingSpeed * Time.deltaTime;
            }
            else
            {
                currentFallingSpeed = 0f;

                if (smallestAnimatorHitDelta > 0)
                    yDelta = Time.deltaTime * smallestAnimatorHitDelta * heightCorrectionSpeed;
            }

            if (yControl)
                transform.position += transform.up * yDelta;

            currentUp = HandleBodyTilt();

            StoreBoneCurrentData();
        }

        void StoreBoneCurrentData()
        {
            foreach (var limb in limbs)
            {
                foreach (var bone in limb.Bones)
                {
                    prevFrame[bone.transform] = (bone.localPosition, bone.localRotation);
                }
            }
        }

        Vector3 HandleBodyTilt()
        {
            Vector3 GetGroundTouchPoint(Vector3 source, Vector3 dir)
            {
                //Debug.DrawRay(source, dir * groundCheckDistance);

                if (Physics.SphereCast(source, 0.02f, dir, out RaycastHit hit, groundCheckDistance, raycastMask))
                {
                    return hit.point;
                }

                return source;
            }

            Vector3 fl = GetGroundTouchPoint(limbs[0].transform.position, Vector3.down);
            Vector3 fr = GetGroundTouchPoint(limbs[1].transform.position, Vector3.down);
            Vector3 bl = GetGroundTouchPoint(limbs[2].transform.position, Vector3.down);
            Vector3 br = GetGroundTouchPoint(limbs[3].transform.position, Vector3.down);

            // only want one axis tilt, averaging left and right limbs
            float fmy = (fl.y + fr.y) * 0.5f;
            fl.y = fmy;
            fr.y = fmy;

            float bmy = (bl.y + br.y) * 0.5f;
            bl.y = bmy;
            br.y = bmy;


            Vector3 sideToSide = ((fr + br) * 0.5f) - ((fl + bl) * 0.5f);
            Vector3 backToFront = ((fl + fr) * 0.5f) - ((bl + br) * 0.5f);

            Vector3 groundUp = Vector3.Cross(backToFront, sideToSide).normalized;
            Debug.DrawRay(transform.position + Vector3.up * 0.1f, groundUp * 0.5f, Color.green);

            // clamping so we dont end up with a spider cat
            float angleWithUp = Vector3.Angle(Vector3.up, groundUp);
            const float maxTiltAngle = 70f;
            if (angleWithUp > maxTiltAngle)
            {
                groundUp = Vector3.Slerp(Vector3.up, groundUp, maxTiltAngle / angleWithUp);
            }

            Vector3 localGroundUp = transform.InverseTransformDirection(groundUp);

            currentTilt = Quaternion.Slerp(currentTilt, Quaternion.FromToRotation(Vector3.up, localGroundUp), Time.deltaTime * tiltCorrectionSpeed);

            tiltBone.localRotation = Quaternion.Slerp(tiltBone.localRotation, currentTilt, tiltFactor);

            return groundUp;
        }
    }
}
