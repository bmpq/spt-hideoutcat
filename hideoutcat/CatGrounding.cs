using UnityEngine;
using tarkin.hideoutcat.InverseKinematics;
using System.Collections.Generic;
#if UNITY_EDITOR
using Vertx.Debugging;
#endif

namespace tarkin.hideoutcat
{
    internal class CatGrounding : MonoBehaviour
    {
        [SerializeField] private LayerMask raycastMask;

        [SerializeField] private BasicIK[] limbs;

        [Range(0f, 1f)]
        [Tooltip("ik influence over animator")]
        [SerializeField] private float factor = 1f;

        [SerializeField] private float heightCorrectionSpeed = 15f;
        [SerializeField] private float fallingSpeed = 0.8f;

        [SerializeField] private float heightCorrectionTime = 0.03f;
        private float inertiaVelocity = 0.0f;

        [Header("body tilt")]
        [SerializeField] private Transform tiltBone;
        private Quaternion currentTilt;
        [Range(0f, 1f)]
        [SerializeField] private float tiltFactor = 1f;

        [SerializeField] private float tiltCorrectionSpeed = 8f;

        [SerializeField] private float groundCheckDistance = 0.5f;
        [SerializeField] private float groundCheckCastRadius = 0.05f;

        [SerializeField] private float maxTiltAngle = 50f;
        [SerializeField] private Vector3 jumpDownCheckOriginOffset = Vector3.zero;
        [SerializeField] private Vector2 jumpDownCheckDir = Vector2.one;
        [SerializeField] private float jumpDownCheckDistance = 1f;

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool showGroundNormalRaycasts;
        [SerializeField] private bool showIKRaycasts;
        [SerializeField] private bool showJumpDownRaycast;
#endif
        private Transform[] ikTargets;

        private float currentYInertia;
        private Vector3 currentUp = Vector3.up;

        public Vector3 HeightCorrectionOffset { get; private set; }
        public bool ShouldFallForward { get; private set; }
        public bool FrontLimbsContact { get; private set; }

        void Start()
        {
            ikTargets = new Transform[limbs.Length];

            for (int i = 0; i < ikTargets.Length; i++)
            {
                ikTargets[i] = new GameObject("IKTarget").transform;
                limbs[i].SetTarget(ikTargets[i]);
                limbs[i].updateMode = UpdateMode.Script;
            }
        }

        public void PerformIKAndCalculateHeightCorrection()
        {
            float smallestAnimatorHitDelta = 1f;

            float slopeAngle = Vector3.Angle(currentUp, Vector3.up);

            bool[] limbHits = new bool[limbs.Length];

            int hitsFound = 0;
            for (int i = 0; i < limbs.Length; i++)
            {
                Vector3 a = limbs[i].transform.position;
                Vector3 b = limbs[i].LastBone.position - limbs[i].targetOffset;
                float animatorDrivenLimbLength = Vector3.Distance(a, b);

                if (i == 0)
                    smallestAnimatorHitDelta = animatorDrivenLimbLength;

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

                    for (int j = 0; j < limbs[i].Bones.Length; j++)
                    {
                        limbs[i].Bones[j].localPosition = Vector3.Lerp(
                            animatorPos[j], limbs[i].Bones[j].localPosition, factor);
                        limbs[i].Bones[j].localRotation = Quaternion.Slerp(
                            animatorRot[j], limbs[i].Bones[j].localRotation, factor);
                    }
#if UNITY_EDITOR
                    if (showIKRaycasts)
                        Debug.DrawLine(a, a + dir * (animatorDrivenLimbLength), Color.red, default, false);
                }
                else
                {
                    if (showIKRaycasts)
                        Debug.DrawLine(a, a + dir * (animatorDrivenLimbLength), Color.white, default, false);
#endif
                }
            }

            currentYInertia += Time.deltaTime * fallingSpeed * Physics.gravity.y;

            if (hitsFound > 0)
            {
                float targetInertia = 0f;

                if (smallestAnimatorHitDelta > 0)
                    targetInertia = smallestAnimatorHitDelta * heightCorrectionSpeed;

                currentYInertia = Mathf.SmoothDamp(
                    currentYInertia,
                    targetInertia,
                    ref inertiaVelocity,
                    heightCorrectionTime
                );
            }
            else
            {
                currentUp = Vector3.up;
            }

            HeightCorrectionOffset = currentUp * currentYInertia * Time.deltaTime;

            currentUp = GetGroundUp(out bool unstable);

            FrontLimbsContact = limbHits[0] || limbHits[1];

            ShouldFallForward = false;
            if (unstable)
            {
                bool isTiltedForward = Vector3.Dot(tiltBone.forward, Vector3.up) < 0;
                bool frontLimbsGrounded = (limbHits[0] && limbHits[1]);

                if (isTiltedForward && !frontLimbsGrounded)
                {
                    Vector3 downcheckOrigin = tiltBone.TransformPoint(jumpDownCheckOriginOffset);
                    Vector3 downcheckDir = tiltBone.forward * jumpDownCheckDir.x + tiltBone.up * jumpDownCheckDir.y;
                    downcheckDir.Normalize();

                    bool highEnough = !Physics.Raycast(downcheckOrigin, downcheckDir, jumpDownCheckDistance, raycastMask);
#if UNITY_EDITOR
                    if (showJumpDownRaycast)
                        Debug.DrawRay(downcheckOrigin, downcheckDir * jumpDownCheckDistance, highEnough ? Color.cyan : Color.red, default, false);
#endif
                    if (highEnough)
                    {
                        ShouldFallForward = true;
                    }
                }
            }
        }

        public void AlignTiltToGround()
        {
            Vector3 localGroundUp = transform.InverseTransformDirection(currentUp);
            currentTilt = Quaternion.Slerp(currentTilt, Quaternion.FromToRotation(Vector3.up, localGroundUp), Time.deltaTime * tiltCorrectionSpeed);
            tiltBone.localRotation = Quaternion.Slerp(tiltBone.localRotation, currentTilt, tiltFactor);
        }

        Vector3 GetGroundUp(out bool unstable)
        {
            unstable = false;

            Vector3 GetGroundTouchPoint(Vector3 source, Vector3 dir)
            {
                Ray ray = new Ray();
                ray.origin = source;
                ray.direction = dir;

                RaycastHit hit;

                if (Physics.SphereCast(source, groundCheckCastRadius, dir, out hit, groundCheckDistance, raycastMask))
                {
#if UNITY_EDITOR
                    if (showGroundNormalRaycasts)
                    {
                        D.raw(new Shape.SphereCast(ray, groundCheckCastRadius, hit));
                        D.raw(new Shape.Sphere(hit.point, 0.02f));
                    }
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
