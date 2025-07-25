using UnityEngine;
using tarkin.hideoutcat.InverseKinematics;
using System.Collections.Generic;
#if UNITY_EDITOR
using Vertx.Debugging;
#endif

namespace tarkin.hideoutcat
{
    internal class CatLimbsIK : MonoBehaviour
    {
        [SerializeField] private LayerMask raycastMask;

        [SerializeField] private BasicIK[] limbs;

        [Range(0f, 1f)]
        [Tooltip("ik influence over animator")]
        [SerializeField] private float factor = 1f;

        [SerializeField] private float heightCorrectionSpeed = 15f;
        [SerializeField] private float fallingSpeed = 0.2f;

        [Header("body tilt")]
        [SerializeField] private Transform tiltBone;
        private Quaternion currentTilt;
        [Range(0f, 1f)]
        public float tiltFactor = 1f;

        [SerializeField] private float tiltCorrectionSpeed = 8f;

        [SerializeField] private float groundCheckDistance = 0.5f;
        [SerializeField] private float groundCheckCastRadius = 0.05f;

        [SerializeField] private float maxTiltAngle = 50f;
        [SerializeField] private Vector3 jumpDownCheckOriginOffset = Vector3.zero;
        [SerializeField] private Vector2 jumpDownCheckDir = Vector2.one;
        [SerializeField] private float jumpDownCheckDistance = 1f;

        private HideoutCat cat;

        private Transform[] ikTargets;

        private Dictionary<Transform, (Vector3, Quaternion)> prevFrame;

        private float currentFallingSpeed;

        private Vector3 currentUp = Vector3.up;

        private bool yControl;

        void Awake()
        {
            cat = GetComponent<HideoutCat>();
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
            yControl = cat.jumpState == HideoutCat.JumpState.None;

            float smallestAnimatorHitDelta = 1f;

            float slopeAngle = Vector3.Angle(currentUp, Vector3.up);

            bool[] limbHits = new bool[limbs.Length]; 

            int hitsFound = 0;
            for (int i = 0; i < limbs.Length; i++)
            {
                Vector3 a = limbs[i].transform.position;
                Vector3 b = limbs[i].LastBone.position - limbs[i].targetOffset;
                float animatorDrivenLimbLength = Vector3.Distance(a, b);

                Vector3 dir = (b - a).normalized;

                if (Physics.Raycast(a, dir, out RaycastHit hit, animatorDrivenLimbLength, raycastMask))
                {
                    hitsFound++;
                    limbHits[i] = true;
                    smallestAnimatorHitDelta = Mathf.Min(smallestAnimatorHitDelta, animatorDrivenLimbLength - hit.distance);

                    ikTargets[i].position = hit.point; 
                    // align the paw with the ground surface
                    Vector3 pawForward = Vector3.ProjectOnPlane(transform.forward, hit.normal);
                    ikTargets[i].rotation = Quaternion.LookRotation(pawForward, hit.normal);

                    Vector3[] animatorPos = new Vector3[limbs[i].Bones.Length];
                    Quaternion[] animatorRot = new Quaternion[limbs[i].Bones.Length];
                    for (int j = 0; j < limbs[i].Bones.Length; j++)
                    {
                        animatorPos[j] = limbs[i].Bones[j].localPosition;
                        animatorRot[j] = limbs[i].Bones[j].localRotation;
                    }

                    limbs[i].SolveIK(); // modifies transforms

                    float residual = limbs[i].GetSolveResidual();
#if UNITY_EDITOR
                    Debug.DrawLine(a, a + dir * (animatorDrivenLimbLength), Color.red, default, false);
                }
                else
                {
                    Debug.DrawLine(a, a + dir * (animatorDrivenLimbLength), Color.white, default, false);
#endif
                }

                // smoothing over two frames
                for (int j = 0; j < limbs[i].Bones.Length; j++)
                {
                    limbs[i].Bones[j].localPosition = Vector3.Lerp(
                        prevFrame[limbs[i].Bones[j].transform].Item1, limbs[i].Bones[j].localPosition, 0.5f);
                    limbs[i].Bones[j].localRotation = Quaternion.Slerp(
                        prevFrame[limbs[i].Bones[j].transform].Item2, limbs[i].Bones[j].localRotation, 0.5f);
                }
            }

            if (yControl)
            {
                float yDelta = 0f;

                bool falling = (hitsFound < 2);

                if (falling)
                {
                    yDelta = Physics.gravity.y * currentFallingSpeed * Time.deltaTime;

                    currentFallingSpeed += Time.deltaTime * fallingSpeed;
                    currentFallingSpeed = Mathf.Clamp(currentFallingSpeed, 0, 0.7f);
                }
                else
                {
                    currentFallingSpeed = 0f;

                    if (smallestAnimatorHitDelta > 0)
                        yDelta = Time.deltaTime * smallestAnimatorHitDelta * heightCorrectionSpeed;
                }

                if (hitsFound == 0)
                    currentUp = Vector3.up;

                transform.position += currentUp * yDelta;
            }

            currentUp = GetGroundUp(out bool unstable);
            Vector3 localGroundUp = transform.InverseTransformDirection(currentUp);
            currentTilt = Quaternion.Slerp(currentTilt, Quaternion.FromToRotation(Vector3.up, localGroundUp), Time.deltaTime * tiltCorrectionSpeed);
            tiltBone.localRotation = Quaternion.Slerp(tiltBone.localRotation, currentTilt, tiltFactor);

            Vector3 downcheckOrigin = tiltBone.TransformPoint(jumpDownCheckOriginOffset);
            Vector3 downcheckDir = tiltBone.forward * jumpDownCheckDir.x + tiltBone.up * jumpDownCheckDir.y;
            downcheckDir.Normalize();

            bool highEnough = !Physics.Raycast(downcheckOrigin, downcheckDir, jumpDownCheckDistance, raycastMask);
#if UNITY_EDITOR
            Debug.DrawRay(downcheckOrigin, downcheckDir * jumpDownCheckDistance, highEnough ? Color.cyan : Color.red, default, false);
#endif

            if (cat.jumpState == HideoutCat.JumpState.AirborneDown)
            {
                if (limbHits[0] || limbHits[1])
                {
                    cat.JumpDownEnd();
                }
            }
            else if (unstable)
            {
                bool isTiltedForward = Vector3.Dot(tiltBone.forward, Vector3.up) < 0;
                bool frontLimbsGrounded = (limbHits[0] && limbHits[1]);


                if (isTiltedForward && !frontLimbsGrounded && highEnough)
                {
                    cat.JumpDownStart();
                }
            }

            StoreBoneCurrentData();
        }

        // after ik solving
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

        Vector3 GetGroundUp(out bool unstable)
        {
            unstable = false;

            if (cat.jumpState != HideoutCat.JumpState.None)
                return Vector3.up;

            Vector3 GetGroundTouchPoint(Vector3 source, Vector3 dir)
            {
                Ray ray = new Ray();
                ray.origin = source;
                ray.direction = dir;

                RaycastHit hit;

                if (Physics.SphereCast(source, groundCheckCastRadius, dir, out hit, groundCheckDistance, raycastMask))
                {
#if UNITY_EDITOR
                    D.raw(new Shape.SphereCast(ray, groundCheckCastRadius, hit));
                    D.raw(new Shape.Sphere(hit.point, 0.02f));
#endif
                    return hit.point;
                }

                return source + Vector3.down * groundCheckDistance;
            }

            Vector3 fl = GetGroundTouchPoint(limbs[0].transform.position, -currentUp);
            Vector3 fr = GetGroundTouchPoint(limbs[1].transform.position, -currentUp);
            Vector3 bl = GetGroundTouchPoint(limbs[2].transform.position, -currentUp);
            Vector3 br = GetGroundTouchPoint(limbs[3].transform.position, -currentUp);

            Vector3 sideToSide = ((fr + br) * 0.5f) - ((fl + bl) * 0.5f);
            Vector3 backToFront = ((fl + fr) * 0.5f) - ((bl + br) * 0.5f);

            Vector3 groundUp = Vector3.Cross(backToFront, sideToSide).normalized;
            Debug.DrawRay(transform.position + Vector3.up * 0.1f, groundUp * 0.5f, Color.green);

            // clamping so we dont end up with a spider cat
            float angleWithUp = Vector3.Angle(Vector3.up, groundUp);
            if (angleWithUp > maxTiltAngle)
            {
                groundUp = Vector3.Slerp(Vector3.up, groundUp, maxTiltAngle / angleWithUp);
                unstable = true;
            }

            return groundUp;
        }
    }
}
