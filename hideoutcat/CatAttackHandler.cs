using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace tarkin.hideoutcat
{
    public class CatAttackHandler : MonoBehaviour
    {
        private Animator animator;
        private HideoutCat cat;
        private CatLookAt lookAt;

        [SerializeField] private Transform visionOrigin;
        [SerializeField] private float visionFOV = 200f;
        [SerializeField] private float visionMaxDistance = 20f;
        [SerializeField] private LayerMask obstacleLayerMask = 1 << 12;
        [Range(0, 0.05f)]
        [SerializeField] private float targetMovingThreshold = 0.01f;

#if UNITY_EDITOR
        [SerializeField] private bool visualizeFOV;
#endif

        private Transform target;
        private Vector3 targetLastSeenPos;

        public enum AttackState
        {
            None,
            Search,
            Track,
            Approach,
            Prime,
            Pounce
        }
        public AttackState CurrentState { get; private set; }

        void SetState(AttackState newState)
        {
            stateTimeElapsed = 0;
            CurrentState = newState;

            if (newState == AttackState.None)
                lookAt.Release();
        }

        private float stateTimeElapsed;

        private readonly int P_POUNCE_PRIMING = Animator.StringToHash("PouncePriming");
        private readonly int P_POUNCE = Animator.StringToHash("Pounce");
        private readonly int P_DISTANCE = Animator.StringToHash("Distance");
        private readonly int P_SNIFF = Animator.StringToHash("Sniff");

        void Awake()
        {
            animator = GetComponent<Animator>();
            cat = GetComponent<HideoutCat>();
            lookAt = GetComponent<CatLookAt>();
        }

        void Update()
        {
            stateTimeElapsed += Time.deltaTime;
            if (target == null)
            {
                SetState(AttackState.None);
                return;
            }

            bool lineOfSight = LineOfSight(out float distanceToTargetFromEyes);

            Vector3 directionToTargetFromRoot = (target.position - transform.position);
            directionToTargetFromRoot.y = 0;
            float angleToTargetFromRoot = Vector3.SignedAngle(transform.forward, directionToTargetFromRoot, Vector3.up);
            float distanceToTargetFromRoot = Vector3.Distance(transform.position, target.position);

            float thrustInput = 0f;
            float turnInput = 0f;
            float crouchInput = 0f;

            switch (CurrentState)
            {
                case AttackState.None:
                    if (lineOfSight && Vector3.SqrMagnitude(target.position - targetLastSeenPos) > (targetMovingThreshold * targetMovingThreshold))
                        SetState(AttackState.Track);
                    break;
                case AttackState.Search:
                    if (lineOfSight)
                        SetState(AttackState.Track);
                    else
                    {
                        crouchInput = stateTimeElapsed < 4f ? 1f : 0f;

                        if (stateTimeElapsed < 1f)
                        {
                            lookAt.LookAt(targetLastSeenPos);
                            break;
                        }
                        else if (stateTimeElapsed > 8f)
                        {
                            SetState(AttackState.None);
                        }

                        float randomLookInterval = UnityExtensions.InverseLerpUnclamped(-1f, 8f, stateTimeElapsed);
                        float randomLookRange = UnityExtensions.InverseLerpUnclamped(-1f, 3f, stateTimeElapsed);

                        if ((int)(stateTimeElapsed / randomLookInterval) > (int)((stateTimeElapsed - Time.deltaTime) / randomLookInterval))
                        {
                            lookAt.LookAt(transform.position + transform.forward +
                                transform.right * Random.Range(-randomLookRange, randomLookRange) +
                                new Vector3(0, Random.Range(-randomLookRange, randomLookRange), 0));
                        }
                    }
                    break;
                case AttackState.Track:
                    if (lineOfSight)
                    {
                        crouchInput = 1f;
                        lookAt.SetLookTarget(target);
                        if (Mathf.Abs(angleToTargetFromRoot) > 30f)
                        {
                            turnInput = Mathf.Clamp(angleToTargetFromRoot, -1f, 1f);
                        }

                        if (distanceToTargetFromRoot < 0.5f)
                        {
                            SetState(AttackState.Prime);
                        }
                        else
                        {
                            if (distanceToTargetFromRoot > 2.4f && distanceToTargetFromRoot < 2.5f)
                                SetState(AttackState.Prime);
                            else
                                SetState(AttackState.Approach);
                        }
                    }
                    else
                    {
                        SetState(AttackState.Search);
                    }
                    break;
                case AttackState.Approach:
                    crouchInput = 1f;
                    if (Mathf.Abs(angleToTargetFromRoot) > 15f)
                        turnInput = Mathf.Clamp(angleToTargetFromRoot, -1f, 1f);

                    if (distanceToTargetFromRoot < 0.22f)
                        thrustInput = -1f;
                    else if (Mathf.Abs(angleToTargetFromRoot) < 30f)
                    {
                        if (distanceToTargetFromRoot > 0.5f && distanceToTargetFromRoot < 1.5f)
                            thrustInput = 1f;
                        else if (distanceToTargetFromRoot >= 1.5f)
                        {
                            thrustInput = distanceToTargetFromRoot;
                            crouchInput = 0f;
                        }
                    }

                    if (distanceToTargetFromRoot > 0.22f && distanceToTargetFromRoot < 0.5f)
                    {
                        SetState(AttackState.Track);
                    }
                    break;
                case AttackState.Prime:
                    if (lineOfSight)
                    {
                        crouchInput = 1f;
                        lookAt.SetLookTarget(target);

                        if (distanceToTargetFromRoot < 0.22f)
                        {
                            SetState(AttackState.Approach);
                            break;
                        }

                        if (Mathf.Abs(angleToTargetFromRoot) > 10f)
                        {
                            turnInput = Mathf.Clamp(angleToTargetFromRoot, -1f, 1f);
                        }
                        else
                        {
                            animator.SetBool(P_POUNCE_PRIMING, true);

                            bool ShouldPounce()
                            {
                                if (distanceToTargetFromRoot < 0.35f) // if close enough, dont wait for priming
                                    return true;

                                if (stateTimeElapsed < 2f)
                                    return false;

                                if (distanceToTargetFromRoot < 0.5f)
                                    return true;

                                if (distanceToTargetFromRoot > 2.4f && distanceToTargetFromRoot < 2.5f) // special long distance jump clip
                                {
                                    return true;
                                }

                                return false;
                            }

                            if (ShouldPounce())
                            {
                                SetState(AttackState.Pounce);
                                animator.SetFloat(P_DISTANCE, distanceToTargetFromRoot);
                                animator.SetTrigger(P_POUNCE);
                            }
                            else
                            {
                                if (stateTimeElapsed > 5f)
                                    SetState(AttackState.Track);
                            }
                        }
                    }
                    else
                    {
                        SetState(AttackState.Search);
                    }
                    break;
                case AttackState.Pounce:
                    crouchInput = 1f;

                    if (stateTimeElapsed > 0.2f)
                    {
                        if (distanceToTargetFromRoot < 0.4f &&
                            Mathf.Abs(angleToTargetFromRoot) < 30f &&
                            Vector3.SqrMagnitude(target.position - targetLastSeenPos) < (targetMovingThreshold * targetMovingThreshold))
                        {
                            lookAt.Release();
                            animator.SetTrigger(P_SNIFF);
                            SetState(AttackState.None);
                        }
                        else
                            SetState(AttackState.Track);
                    }
                    break;
            }

            if (CurrentState != AttackState.Prime && CurrentState != AttackState.Pounce)
                animator.SetBool(P_POUNCE_PRIMING, false);

            if (lineOfSight)
            {
                Debug.DrawLine(visionOrigin.position, target.position, Color.green);
                targetLastSeenPos = target.position;
            }

            cat.MovementInput = new Vector2(turnInput, thrustInput);
            cat.CrouchInput = crouchInput;
        }

        bool LineOfSight(out float distanceToTargetFromEyes)
        {
            Vector3 directionToTarget = (target.position - visionOrigin.position);
            distanceToTargetFromEyes = directionToTarget.magnitude;

            if (distanceToTargetFromEyes > visionMaxDistance)
                return false;

            if (Vector3.Angle(visionOrigin.forward, directionToTarget.normalized) > visionFOV / 2)
                return false;

            if (Physics.Raycast(visionOrigin.position, directionToTarget.normalized, out RaycastHit hit, distanceToTargetFromEyes - 0.01f, obstacleLayerMask))
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
            Handles.Label(transform.position, CurrentState.ToString());

            if (visualizeFOV)
                UnityEditorExtensions.DrawVolumetricCone(visionOrigin.position, visionOrigin.forward, visionFOV / 2f, 0.3f, new Color(1f, 1f, 0.1f, 0.2f));
        }
#endif
    }
}
